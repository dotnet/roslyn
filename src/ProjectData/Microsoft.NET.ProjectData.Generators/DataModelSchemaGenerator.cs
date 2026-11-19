// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.NET.ProjectData.Generators;

/// <summary>
/// Reads <c>project-data-schema.json</c> and generates strongly-typed constants
/// and accessors for the Data Model schema.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DataModelSchemaGenerator : IIncrementalGenerator
{
	private const string SchemaFileName = "project-data-schema.json";

	private static readonly DiagnosticDescriptor InvalidSchema = new(
		"PDG001",
		"Invalid ProjectData schema",
		"ProjectData schema '{0}' is invalid: {1}",
		"ProjectData",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

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
					if (schemaFile is not null)
					{
						ReportInvalidSchema(ctx, file.Path, sourceText: null, $"multiple additional files named '{SchemaFileName}' were provided.");
						return;
					}

					schemaFile = file;
				}
			}

			if (schemaFile is null)
			{
				ReportInvalidSchema(ctx, SchemaFileName, sourceText: null, "the required schema additional file was not provided.");
				return;
			}

			SourceText? sourceText;
			try
			{
				sourceText = schemaFile.GetText(ctx.CancellationToken);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				ReportInvalidSchema(ctx, schemaFile.Path, sourceText: null, $"the schema file could not be read: {ex.Message}");
				return;
			}

			if (sourceText is null)
			{
				ReportInvalidSchema(ctx, schemaFile.Path, sourceText: null, "the schema file could not be read.");
				return;
			}

			Schema schema;
			try
			{
				schema = ParseSchema(sourceText.ToString());
			}
			catch (JsonException ex)
			{
				ReportInvalidSchema(
					ctx,
					schemaFile.Path,
					sourceText,
					$"the file does not contain valid JSON (line {ex.LineNumber.GetValueOrDefault() + 1}, byte {ex.BytePositionInLine.GetValueOrDefault() + 1}).");
				return;
			}
			catch (SchemaValidationException ex)
			{
				ReportInvalidSchema(ctx, schemaFile.Path, sourceText, ex.Message);
				return;
			}

			// Derive namespace from the consuming project's assembly name.
			// CSDevKit → CSDevKit.Contracts.DataModel (its existing brokered DTO convention)
			// ProjectData reader/tasks → their root namespace
			string assemblyName = compilation.AssemblyName ?? "";
			string namespaceName = assemblyName switch
			{
				"CSDevKit" => "CSDevKit.Contracts.DataModel",
				_ => assemblyName,
			};

			ctx.AddSource("ProjectProperties.g.cs", GeneratorSourceText.From(GenerateProjectProperties(schema, namespaceName)));
			ctx.AddSource("ProjectItems.g.cs", GeneratorSourceText.From(GenerateProjectItems(schema, namespaceName)));

			// Wire-format constants for the on-disk .lscache file. Emitted into the same
			// namespace as the other generated constants so the writer and reader can
			// reference a single source of truth and a rename cannot silently drop a
			// section.
			ctx.AddSource("CacheFormat.g.cs", GeneratorSourceText.From(GenerateCacheFormat(schema, namespaceName)));
			ctx.AddSource("PathSentinels.g.cs", GeneratorSourceText.From(GeneratePathSentinels(schema.PathSentinels, namespaceName)));

			// Only emit TypedProperties when the consuming project has KeyValueCollection;
			// the generated accessors cannot compile without the immutable model types.
			bool hasKeyValueCollection = compilation.GetTypeByMetadataName(
				$"{namespaceName}.KeyValueCollection") is not null;
			if (hasKeyValueCollection)
			{
				ctx.AddSource("TypedProperties.g.cs", GeneratorSourceText.From(GenerateTypedProperties(schema, namespaceName)));
			}
		});
	}

	private static void ReportInvalidSchema(SourceProductionContext context, string path, SourceText? sourceText, string detail)
	{
		Location location = sourceText is null
			? Location.None
			: Location.Create(
				path,
				new TextSpan(0, 0),
				new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
		context.ReportDiagnostic(Diagnostic.Create(InvalidSchema, location, path, detail));
	}

	private static Schema ParseSchema(string json)
	{
		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement schemaRoot = RequireKind(document.RootElement, JsonValueKind.Object, "the schema root");
		ValidateNoDuplicateFields(schemaRoot, "the schema root");

		JsonElement propertiesElement = RequireProperty(schemaRoot, "properties", JsonValueKind.Object);
		ValidateNoDuplicateFields(propertiesElement, "'properties'");
		JsonElement requiredPropertiesElement = RequireProperty(propertiesElement, "required", JsonValueKind.Array, "properties");
		JsonElement optionalPropertiesElement = RequireProperty(propertiesElement, "optional", JsonValueKind.Array, "properties");

		HashSet<string> propertyNames = new(StringComparer.OrdinalIgnoreCase);
		List<PropertyDef> requiredProperties = ParseProperties(requiredPropertiesElement, "properties.required", propertyNames);
		List<PropertyDef> optionalProperties = ParseProperties(optionalPropertiesElement, "properties.optional", propertyNames);
		if (requiredProperties.Count == 0 && optionalProperties.Count == 0)
			throw Invalid("Field 'properties' must define at least one property.");

		JsonElement itemsElement = RequireProperty(schemaRoot, "items", JsonValueKind.Object);
		ValidateNoDuplicateFields(itemsElement, "'items'");
		List<ItemDef> items = ParseItems(itemsElement);
		HashSet<string> itemNames = new(items.Select(static item => item.Name), StringComparer.Ordinal);

		JsonElement cacheFormatElement = RequireProperty(schemaRoot, "cacheFormat", JsonValueKind.Object);
		ValidateNoDuplicateFields(cacheFormatElement, "'cacheFormat'");
		CacheFormatDef cacheFormat = ParseCacheFormat(cacheFormatElement, itemNames);

		JsonElement pathSentinelsElement = RequireProperty(schemaRoot, "pathSentinels", JsonValueKind.Object);
		ValidateNoDuplicateFields(pathSentinelsElement, "'pathSentinels'");
		PathSentinelsDef pathSentinels = ParsePathSentinels(pathSentinelsElement);

		return new(requiredProperties, optionalProperties, items, cacheFormat, pathSentinels);
	}

	private static List<PropertyDef> ParseProperties(JsonElement properties, string path, HashSet<string> propertyNames)
	{
		List<PropertyDef> result = [];
		int index = 0;
		foreach (JsonElement property in properties.EnumerateArray())
		{
			string propertyPath = $"{path}[{index}]";
			RequireKind(property, JsonValueKind.Object, $"field '{propertyPath}'");
			ValidateNoDuplicateFields(property, $"field '{propertyPath}'");

			string name = RequireIdentifier(property, "name", propertyPath);
			if (name is "ProjectProperties" or "Required" or "All" or "TypedProperties" or "inner")
				throw Invalid($"Field '{propertyPath}.name' cannot be '{name}' because that name is reserved by the generated ProjectProperties or TypedProperties type.");
			if (!propertyNames.Add(name))
				throw Invalid($"Property name '{name}' is duplicated across 'properties.required' and 'properties.optional'.");

			_ = GetOptionalNonEmptyString(property, "value", propertyPath);
			result.Add(new(name, GetOptionalDescription(property, propertyPath)));
			index++;
		}

		return result;
	}

	private static List<ItemDef> ParseItems(JsonElement itemsElement)
	{
		List<ItemDef> items = [];
		HashSet<string> generatedNames = new(StringComparer.OrdinalIgnoreCase);
		foreach (JsonProperty item in itemsElement.EnumerateObject())
		{
			string itemPath = $"items.{item.Name}";
			ValidateIdentifier(item.Name, "Item name");
			if (item.Name is "ProjectItems" or "AllItemTypes" or "RequiredItemTypes" or "AllMetadata" or "MetadataByItemType")
				throw Invalid($"Item name '{item.Name}' is reserved by the generated ProjectItems type.");
			if (!generatedNames.Add(item.Name))
				throw Invalid($"Item name '{item.Name}' is duplicated.");

			RequireKind(item.Value, JsonValueKind.Object, $"field '{itemPath}'");
			ValidateNoDuplicateFields(item.Value, $"field '{itemPath}'");

			bool isRequired = GetOptionalBoolean(item.Value, "required", itemPath);
			List<string> metadata = GetOptionalMetadata(item.Value, itemPath, item.Name);
			items.Add(new(item.Name, GetOptionalDescription(item.Value, itemPath), isRequired, metadata));
		}

		if (items.Count == 0)
			throw Invalid("Field 'items' must define at least one item.");

		return items;
	}

	private static List<string> GetOptionalMetadata(JsonElement item, string itemPath, string itemName)
	{
		if (!item.TryGetProperty("metadata", out JsonElement metadataElement))
			return [];

		RequireKind(metadataElement, JsonValueKind.Array, $"field '{itemPath}.metadata'");
		List<string> metadata = [];
		HashSet<string> metadataNames = new(StringComparer.OrdinalIgnoreCase);
		HashSet<string> generatedNames = new(StringComparer.Ordinal) { "ItemType", itemName };
		int index = 0;
		foreach (JsonElement value in metadataElement.EnumerateArray())
		{
			string path = $"{itemPath}.metadata[{index}]";
			string name = RequireNonEmptyString(value, path);
			if (!metadataNames.Add(name))
				throw Invalid($"Metadata name '{name}' is duplicated in field '{itemPath}.metadata'.");

			string generatedName = PascalCase(name);
			ValidateIdentifier(generatedName, $"Field '{path}'");
			if (!generatedNames.Add(generatedName))
				throw Invalid($"Field '{path}' produces duplicate generated member name '{generatedName}'.");

			metadata.Add(name);
			index++;
		}

		return metadata;
	}

	private static CacheFormatDef ParseCacheFormat(JsonElement cacheFormat, HashSet<string> itemNames)
	{
		string versionHeader = RequireToken(cacheFormat, "versionHeader", "cacheFormat");
		if (!IsValidVersionHeader(versionHeader))
			throw Invalid("Field 'cacheFormat.versionHeader' must have the form 'version=<major>[.<minor>]' using non-negative integers.");

		string hashHeaderPrefix = RequireToken(cacheFormat, "hashHeaderPrefix", "cacheFormat");
		string sliceSeparator = RequireToken(cacheFormat, "sliceSeparator", "cacheFormat");
		string sectionOpen = RequireToken(cacheFormat, "sectionOpen", "cacheFormat");
		string sectionClose = RequireToken(cacheFormat, "sectionClose", "cacheFormat");
		string commentChar = RequireToken(cacheFormat, "commentChar", "cacheFormat");
		if (commentChar.Length != 1)
			throw Invalid("Field 'cacheFormat.commentChar' must contain exactly one character.");

		string projectHeaderPrefix = RequireToken(cacheFormat, "projectHeaderPrefix", "cacheFormat");
		string languagePrefix = RequireToken(cacheFormat, "languagePrefix", "cacheFormat");
		string primaryMarker = RequireToken(cacheFormat, "primaryMarker", "cacheFormat");
		string lastDtbSucceededMarker = RequireToken(cacheFormat, "lastDtbSucceededMarker", "cacheFormat");

		if (sectionOpen == sectionClose)
			throw Invalid("Fields 'cacheFormat.sectionOpen' and 'cacheFormat.sectionClose' must be different.");
		if (projectHeaderPrefix == languagePrefix)
			throw Invalid("Fields 'cacheFormat.projectHeaderPrefix' and 'cacheFormat.languagePrefix' must be different.");
		if (primaryMarker == lastDtbSucceededMarker)
			throw Invalid("Fields 'cacheFormat.primaryMarker' and 'cacheFormat.lastDtbSucceededMarker' must be different.");

		JsonElement sectionsElement = RequireProperty(cacheFormat, "sections", JsonValueKind.Array, "cacheFormat");
		List<SectionDef> sections = [];
		HashSet<string> sectionNames = new(StringComparer.Ordinal);
		HashSet<string> generatedNames = new(StringComparer.Ordinal) { "Sections", "All", "MetadataBySection" };
		int index = 0;
		foreach (JsonElement section in sectionsElement.EnumerateArray())
		{
			string sectionPath = $"cacheFormat.sections[{index}]";
			RequireKind(section, JsonValueKind.Object, $"field '{sectionPath}'");
			ValidateNoDuplicateFields(section, $"field '{sectionPath}'");

			string name = RequireToken(section, "name", sectionPath);
			ValidateIdentifier(PascalCase(name), $"Field '{sectionPath}.name'");
			if (!sectionNames.Add(name))
				throw Invalid($"Section name '{name}' is duplicated in field 'cacheFormat.sections'.");

			string generatedName = PascalCase(name);
			if (!generatedNames.Add(generatedName))
				throw Invalid($"Field '{sectionPath}.name' produces duplicate generated member name '{generatedName}'.");

			string? itemType = GetOptionalNonEmptyString(section, "itemType", sectionPath);
			if (itemType is not null && !itemNames.Contains(itemType))
				throw Invalid($"Field '{sectionPath}.itemType' references unknown item type '{itemType}'.");

			sections.Add(new(name, GetOptionalDescription(section, sectionPath), itemType));
			index++;
		}

		if (sections.Count == 0)
			throw Invalid("Field 'cacheFormat.sections' must define at least one section.");

		return new(
			versionHeader,
			hashHeaderPrefix,
			sliceSeparator,
			sectionOpen,
			sectionClose,
			commentChar,
			projectHeaderPrefix,
			languagePrefix,
			primaryMarker,
			lastDtbSucceededMarker,
			sections);
	}

	private static PathSentinelsDef ParsePathSentinels(JsonElement pathSentinels)
	{
		List<(string Name, string Value)> entries = [];
		HashSet<string> generatedNames = new(StringComparer.Ordinal) { "PathSentinels" };
		HashSet<string> values = new(StringComparer.Ordinal);
		foreach (JsonProperty entry in pathSentinels.EnumerateObject())
		{
			if (entry.Name.StartsWith("$", StringComparison.Ordinal))
				continue;

			ValidateIdentifier(PascalCase(entry.Name), "Path sentinel name");
			string generatedName = PascalCase(entry.Name);
			if (!generatedNames.Add(generatedName))
				throw Invalid($"Path sentinel name '{entry.Name}' produces duplicate generated member name '{generatedName}'.");

			string value = RequireNonEmptyString(entry.Value, $"pathSentinels.{entry.Name}");
			ValidateToken(value, $"Field 'pathSentinels.{entry.Name}'");
			if (!values.Add(value))
				throw Invalid($"Path sentinel value '{value}' is duplicated.");

			entries.Add((entry.Name, value));
		}

		if (entries.Count == 0)
			throw Invalid("Field 'pathSentinels' must define at least one sentinel.");

		return new(entries);
	}

	private static JsonElement RequireProperty(JsonElement parent, string name, JsonValueKind kind, string? parentPath = null)
	{
		string path = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}.{name}";
		if (!parent.TryGetProperty(name, out JsonElement value))
			throw Invalid($"Required field '{path}' is missing.");

		return RequireKind(value, kind, $"field '{path}'");
	}

	private static JsonElement RequireKind(JsonElement value, JsonValueKind kind, string subject)
	{
		if (value.ValueKind != kind)
			throw Invalid($"{char.ToUpperInvariant(subject[0]) + subject.Substring(1)} must be a JSON {GetJsonKindName(kind)}, but found {GetJsonKindName(value.ValueKind)}.");
		return value;
	}

	private static string RequireIdentifier(JsonElement parent, string name, string parentPath)
	{
		string path = $"{parentPath}.{name}";
		string value = RequireToken(parent, name, parentPath);
		ValidateIdentifier(value, $"Field '{path}'");
		return value;
	}

	private static string RequireToken(JsonElement parent, string name, string parentPath)
	{
		string path = $"{parentPath}.{name}";
		JsonElement value = RequireProperty(parent, name, JsonValueKind.String, parentPath);
		string result = RequireNonEmptyString(value, path);
		ValidateToken(result, $"Field '{path}'");
		return result;
	}

	private static string RequireNonEmptyString(JsonElement value, string path)
	{
		RequireKind(value, JsonValueKind.String, $"field '{path}'");
		string? result = value.GetString();
		if (string.IsNullOrWhiteSpace(result))
			throw Invalid($"Field '{path}' must be a non-empty string.");
		return result!;
	}

	private static string? GetOptionalNonEmptyString(JsonElement parent, string name, string parentPath)
	{
		if (!parent.TryGetProperty(name, out JsonElement value))
			return null;

		string path = $"{parentPath}.{name}";
		string result = RequireNonEmptyString(value, path);
		ValidateToken(result, $"Field '{path}'");
		return result;
	}

	private static string GetOptionalDescription(JsonElement parent, string parentPath)
	{
		if (!parent.TryGetProperty("description", out JsonElement value))
			return "";

		string path = $"{parentPath}.description";
		RequireKind(value, JsonValueKind.String, $"field '{path}'");
		string description = value.GetString()!;
		if (description.IndexOf('\r') >= 0 || description.IndexOf('\n') >= 0)
			throw Invalid($"Field '{path}' cannot contain line breaks.");
		ValidateToken(description, $"Field '{path}'");
		return description;
	}

	private static bool GetOptionalBoolean(JsonElement parent, string name, string parentPath)
	{
		if (!parent.TryGetProperty(name, out JsonElement value))
			return false;

		string path = $"{parentPath}.{name}";
		RequireKind(value, JsonValueKind.True, $"field '{path}'", JsonValueKind.False);
		return value.GetBoolean();
	}

	private static JsonElement RequireKind(JsonElement value, JsonValueKind firstKind, string subject, JsonValueKind secondKind)
	{
		if (value.ValueKind != firstKind && value.ValueKind != secondKind)
			throw Invalid($"{char.ToUpperInvariant(subject[0]) + subject.Substring(1)} must be a JSON boolean, but found {GetJsonKindName(value.ValueKind)}.");
		return value;
	}

	private static void ValidateNoDuplicateFields(JsonElement element, string subject)
	{
		HashSet<string> names = new(StringComparer.Ordinal);
		foreach (JsonProperty property in element.EnumerateObject())
		{
			if (!names.Add(property.Name))
				throw Invalid($"{char.ToUpperInvariant(subject[0]) + subject.Substring(1)} contains duplicate field '{property.Name}'.");
		}
	}

	private static void ValidateIdentifier(string value, string subject)
	{
		if (string.IsNullOrWhiteSpace(value)
			|| !IsIdentifierStart(value[0])
			|| value.Skip(1).Any(static c => !IsIdentifierPart(c))
			|| IsReservedKeyword(value))
			throw Invalid($"{subject} must be a valid, non-keyword C# identifier, but found '{value}'.");
	}

	private static bool IsIdentifierStart(char value)
		=> value == '_' || char.GetUnicodeCategory(value) is
			UnicodeCategory.UppercaseLetter or
			UnicodeCategory.LowercaseLetter or
			UnicodeCategory.TitlecaseLetter or
			UnicodeCategory.ModifierLetter or
			UnicodeCategory.OtherLetter or
			UnicodeCategory.LetterNumber;

	private static bool IsIdentifierPart(char value)
		=> IsIdentifierStart(value) || char.GetUnicodeCategory(value) is
			UnicodeCategory.DecimalDigitNumber or
			UnicodeCategory.ConnectorPunctuation or
			UnicodeCategory.SpacingCombiningMark or
			UnicodeCategory.NonSpacingMark or
			UnicodeCategory.Format;

	private static bool IsReservedKeyword(string value)
		=> value is
			"abstract" or "as" or "base" or "bool" or "break" or "byte" or
			"case" or "catch" or "char" or "checked" or "class" or "const" or "continue" or
			"decimal" or "default" or "delegate" or "do" or "double" or
			"else" or "enum" or "event" or "explicit" or "extern" or
			"false" or "finally" or "fixed" or "float" or "for" or "foreach" or
			"goto" or "if" or "implicit" or "in" or "int" or "interface" or "internal" or "is" or
			"lock" or "long" or "namespace" or "new" or "null" or "object" or "operator" or "out" or "override" or
			"params" or "private" or "protected" or "public" or "readonly" or "ref" or "return" or
			"sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or "static" or "string" or "struct" or "switch" or
			"this" or "throw" or "true" or "try" or "typeof" or "uint" or "ulong" or "unchecked" or "unsafe" or "ushort" or "using" or
			"virtual" or "void" or "volatile" or "while";

	private static void ValidateToken(string value, string subject)
	{
		foreach (char c in value)
		{
			if (char.IsControl(c) || c is '\u2028' or '\u2029')
				throw Invalid($"{subject} cannot contain control characters or Unicode line separators.");
		}
	}

	private static bool IsValidVersionHeader(string value)
	{
		const string Prefix = "version=";
		if (!value.StartsWith(Prefix, StringComparison.Ordinal))
			return false;

		ReadOnlySpan<char> version = value.AsSpan(Prefix.Length);
		int separator = version.IndexOf('.');
		if (separator < 0)
			return TryParseNonNegativeInteger(version);
		if (separator == 0 || separator == version.Length - 1 || version.Slice(separator + 1).IndexOf('.') >= 0)
			return false;
		return TryParseNonNegativeInteger(version.Slice(0, separator))
			&& TryParseNonNegativeInteger(version.Slice(separator + 1));
	}

	private static bool TryParseNonNegativeInteger(ReadOnlySpan<char> value)
	{
		if (value.Length == 0)
			return false;
		foreach (char c in value)
		{
			if (c is < '0' or > '9')
				return false;
		}
		return int.TryParse(value.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out _);
	}

	private static string GetJsonKindName(JsonValueKind kind)
		=> kind switch
		{
			JsonValueKind.Object => "object",
			JsonValueKind.Array => "array",
			JsonValueKind.String => "string",
			JsonValueKind.Number => "number",
			JsonValueKind.True or JsonValueKind.False => "boolean",
			JsonValueKind.Null => "null",
			JsonValueKind.Undefined => "undefined value",
			_ => kind.ToString(),
		};

	private static SchemaValidationException Invalid(string message) => new(message);

	private static string GenerateProjectProperties(Schema schema, string namespaceName)
	{
		StringBuilder sb = new();
		AppendLines(
			sb,
			$$"""
			// <auto-generated/>
			// Generated from project-data-schema.json by DataModelSchemaGenerator.

			namespace {{namespaceName}};

			/// <summary>Strongly-typed constants for all Data Model property names.</summary>
			internal static class ProjectProperties
			{
			""");

		foreach (PropertyDef property in schema.Required)
		{
			sb.AppendLine($"\t/// <summary>{EscapeXml(property.Description)}</summary>");
			sb.AppendLine($"\tpublic const string {property.Name} = \"{property.Name}\";");
		}
		sb.AppendLine();
		foreach (PropertyDef property in schema.Optional)
		{
			sb.AppendLine($"\t/// <summary>{EscapeXml(property.Description)}</summary>");
			sb.AppendLine($"\tpublic const string {property.Name} = \"{property.Name}\";");
		}

		sb.AppendLine();
		sb.Append("\tpublic static readonly string[] Required = [");
		sb.Append(string.Join(", ", schema.Required.Select(static property => $"\"{property.Name}\"")));
		sb.AppendLine("];");

		sb.AppendLine();
		AppendLines(
			sb,
			"""
				/// <summary>
				/// All Data Model property names. Every property is exported into the cache file's
				/// <c>[properties]</c> section by the writer (the generated <c>_ProjectDataProperties</c>
				/// MSBuild allow-list is the same set). This is also the forward-compatibility
				/// "known [properties] key" set: a key NOT listed here is treated as data authored by a
				/// newer writer and preserved losslessly rather than dropped.
				/// </summary>
			""");
		sb.Append("\tpublic static readonly string[] All = [");
		sb.Append(string.Join(", ", schema.Required.Concat(schema.Optional).Select(static property => $"\"{property.Name}\"")));
		sb.AppendLine("];");

		sb.AppendLine("}");
		return sb.ToString();
	}

	private static string GenerateProjectItems(Schema schema, string namespaceName)
	{
		StringBuilder sb = new();
		AppendLines(
			sb,
			$$"""
			// <auto-generated/>
			// Generated from project-data-schema.json by DataModelSchemaGenerator.

			using System.Collections.Generic;

			namespace {{namespaceName}};

			/// <summary>Strongly-typed constants for Data Model item types and metadata.</summary>
			internal static class ProjectItems
			{
			""");

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
				foreach (string metadataName in item.Metadata)
					sb.AppendLine($"\t\tpublic const string {char.ToUpperInvariant(metadataName[0]) + metadataName.Substring(1)} = \"{metadataName}\";");
				sb.AppendLine("\t}");
			}
			sb.AppendLine();
		}

		sb.Append("\tpublic static readonly string[] AllItemTypes = [");
		sb.Append(string.Join(", ", schema.Items.Select(static item => $"\"{item.Name}\"")));
		sb.AppendLine("];");

		sb.AppendLine();
		sb.AppendLine("\t/// <summary>Item types that must be present (even if empty) in every valid snapshot.</summary>");
		sb.Append("\tpublic static readonly string[] RequiredItemTypes = [");
		sb.Append(string.Join(", ", schema.Items.Where(static item => item.IsRequired).Select(static item => $"\"{item.Name}\"")));
		sb.AppendLine("];");

		sb.AppendLine();
		AppendLines(
			sb,
			"""
				/// <summary>
				/// Every metadata key the writer can emit on any item (the union across all item
				/// types). Used by forward-compatibility preservation to tell a known <c>@metadata</c>
				/// line from one written by a newer version that must be carried through losslessly.
				/// </summary>
			""");
		sb.Append("\tpublic static readonly string[] AllMetadata = [");
		sb.Append(string.Join(", ", schema.Items
			.SelectMany(static item => item.Metadata)
			.Distinct(StringComparer.Ordinal)
			.Select(static metadataName => $"\"{metadataName}\"")));
		sb.AppendLine("];");

		sb.AppendLine();
		AppendLines(
			sb,
			"""
				/// <summary>
				/// Metadata keys the writer can emit, grouped by the item type that carries them (NOT a
				/// flattened union). Forward-compatibility preservation uses this to decide what is
				/// "known" <em>per item type</em>, so a newer version that reuses an existing metadata
				/// name on a different item type is still treated as unknown and carried through losslessly.
				/// </summary>
				public static readonly Dictionary<string, string[]> MetadataByItemType = new(System.StringComparer.Ordinal)
				{
			""");
		foreach (ItemDef item in schema.Items.Where(static item => item.Metadata.Count > 0))
		{
			string metadataValues = string.Join(", ", item.Metadata.Select(static metadataName => $"\"{metadataName}\""));
			sb.AppendLine($"\t\t[\"{item.Name}\"] = [{metadataValues}],");
		}

		sb.AppendLine("\t};");

		sb.AppendLine("}");
		return sb.ToString();
	}

	private static string GenerateTypedProperties(Schema schema, string namespaceName)
	{
		StringBuilder sb = new();
		AppendLines(
			sb,
			$$"""
			// <auto-generated/>
			#nullable enable
			// Generated from project-data-schema.json by DataModelSchemaGenerator.

			using System;

			namespace {{namespaceName}};

			/// <summary>Strongly-typed accessor over <see cref="KeyValueCollection"/>.</summary>
			internal readonly ref struct TypedProperties
			{
				private readonly KeyValueCollection inner;
				public TypedProperties(KeyValueCollection inner) => this.inner = inner;

			""");

		foreach (PropertyDef property in schema.Required)
		{
			sb.AppendLine($"\t/// <summary>{EscapeXml(property.Description)}</summary>");
			sb.AppendLine($"\tpublic string {property.Name} => this.inner[ProjectProperties.{property.Name}] ?? throw new InvalidOperationException(\"Required Data Model property '\" + ProjectProperties.{property.Name} + \"' is missing. The .lscache file may be incomplete or the property merge failed.\");");
		}
		sb.AppendLine();
		foreach (PropertyDef property in schema.Optional)
		{
			sb.AppendLine($"\t/// <summary>{EscapeXml(property.Description)}</summary>");
			sb.AppendLine($"\tpublic string? {property.Name} => this.inner[ProjectProperties.{property.Name}];");
		}

		AppendLines(
			sb,
			"""
			}

			internal static class TypedPropertiesExtensions
			{
				public static TypedProperties Typed(this ProjectDataSnapshot snapshot) => new(snapshot.Properties);
			}
			""");
		return sb.ToString();
	}

	private static string EscapeXml(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

	private static string EscapeForCSharpString(string value)
		=> value.Replace("\\", "\\\\").Replace("\"", "\\\"");

	private static string EscapeForCSharpChar(string value)
		=> value switch
		{
			"'" => "\\'",
			"\\" => "\\\\",
			_ => value,
		};

	private static string PascalCase(string name)
	{
		if (string.IsNullOrEmpty(name))
			return name;
		return char.ToUpperInvariant(name[0]) + name.Substring(1);
	}

	private static string GenerateCacheFormat(Schema schema, string namespaceName)
	{
		CacheFormatDef cacheFormat = schema.CacheFormat;
		StringBuilder sb = new();
		AppendLines(
			sb,
			$$"""
			// <auto-generated/>
			// Generated from project-data-schema.json by DataModelSchemaGenerator.

			using System.Collections.Generic;

			namespace {{namespaceName}};

			/// <summary>
			/// Wire-format tokens for the on-disk <c>.lscache</c> file. Shared by the writer
			/// (<c>Microsoft.NET.ProjectData.Tasks</c>) and the reader
			/// (<c>Microsoft.NET.ProjectData</c>) so renames cannot silently drop a section.
			/// </summary>
			internal static class CacheFormat
			{
			""");
		sb.AppendLine($"\tpublic const string VersionHeader = \"{EscapeForCSharpString(cacheFormat.VersionHeader)}\";");
		sb.AppendLine($"\tpublic const string HashHeaderPrefix = \"{EscapeForCSharpString(cacheFormat.HashHeaderPrefix)}\";");
		sb.AppendLine($"\tpublic const string SliceSeparator = \"{EscapeForCSharpString(cacheFormat.SliceSeparator)}\";");
		sb.AppendLine($"\tpublic const string SectionOpen = \"{EscapeForCSharpString(cacheFormat.SectionOpen)}\";");
		sb.AppendLine($"\tpublic const string SectionClose = \"{EscapeForCSharpString(cacheFormat.SectionClose)}\";");
		sb.AppendLine($"\tpublic const char CommentChar = '{EscapeForCSharpChar(cacheFormat.CommentChar)}';");
		sb.AppendLine($"\tpublic const string ProjectHeaderPrefix = \"{EscapeForCSharpString(cacheFormat.ProjectHeaderPrefix)}\";");
		sb.AppendLine($"\tpublic const string LanguagePrefix = \"{EscapeForCSharpString(cacheFormat.LanguagePrefix)}\";");
		sb.AppendLine($"\tpublic const string PrimaryMarker = \"{EscapeForCSharpString(cacheFormat.PrimaryMarker)}\";");
		sb.AppendLine($"\tpublic const string LastDtbSucceededMarker = \"{EscapeForCSharpString(cacheFormat.LastDtbSucceededMarker)}\";");
		sb.AppendLine();
		AppendLines(
			sb,
			"""
				/// <summary>Section names emitted as <c>[name]</c> by the writer and matched without brackets by the reader.</summary>
				public static class Sections
				{
			""");
		foreach (SectionDef section in cacheFormat.Sections)
		{
			if (!string.IsNullOrEmpty(section.Description))
				sb.AppendLine($"\t\t/// <summary>{EscapeXml(section.Description)}</summary>");
			sb.AppendLine($"\t\tpublic const string {PascalCase(section.Name)} = \"{EscapeForCSharpString(section.Name)}\";");
		}
		sb.AppendLine();
		sb.Append("\t\tpublic static readonly string[] All = [");
		sb.Append(string.Join(", ", cacheFormat.Sections.Select(static section => $"\"{EscapeForCSharpString(section.Name)}\"")));
		sb.AppendLine("];");
		sb.AppendLine();
		AppendLines(
			sb,
			"""
					/// <summary>
					/// Item <c>@metadata</c> keys the writer can emit, keyed by the WIRE section that carries
					/// them (resolved through the section's <c>itemType</c> link in the schema). Used by
					/// forward-compatibility preservation to judge "known" metadata <em>per section</em>, so a
					/// newer writer that reuses an existing metadata name on a different item type is still
					/// preserved rather than mistaken for regenerable data. Only sections whose item type
					/// emits metadata appear here; a section absent from the map emits no metadata.
					/// </summary>
					public static readonly Dictionary<string, string[]> MetadataBySection = new(System.StringComparer.Ordinal)
					{
			""");
		foreach (SectionDef section in cacheFormat.Sections)
		{
			if (section.ItemType is null)
				continue;
			ItemDef? item = schema.Items.FirstOrDefault(item => item.Name == section.ItemType);
			if (item is null || item.Metadata.Count == 0)
				continue;
			string metadataValues = string.Join(", ", item.Metadata.Select(static metadataName => $"\"{EscapeForCSharpString(metadataName)}\""));
			sb.AppendLine($"\t\t\t[\"{EscapeForCSharpString(section.Name)}\"] = [{metadataValues}],");
		}
		AppendLines(
			sb,
			"""
					};
				}

				/// <summary>Returns <paramref name="name"/> wrapped in section delimiters, e.g. <c>[sourceFiles]</c>.</summary>
				public static string SectionHeader(string name) => SectionOpen + name + SectionClose;
			}
			""");
		return sb.ToString();
	}

	private static string GeneratePathSentinels(PathSentinelsDef pathSentinels, string namespaceName)
	{
		StringBuilder sb = new();
		AppendLines(
			sb,
			$$"""
			// <auto-generated/>
			// Generated from project-data-schema.json by DataModelSchemaGenerator.

			namespace {{namespaceName}};

			/// <summary>
			/// Sentinel prefixes embedded in encoded paths in the <c>.lscache</c> file.
			/// The writer rewrites version-specific or location-specific paths to these sentinels
			/// so the cache is portable across machines and SDK upgrades; the reader resolves them
			/// via <c>CachePathResolver</c>.
			/// </summary>
			internal static class PathSentinels
			{
			""");
		foreach ((string name, string value) in pathSentinels.Entries)
		{
			AppendLines(
				sb, $"\tpublic const string {PascalCase(name)} = \"{EscapeForCSharpString(value)}\";");
		}
		sb.AppendLine("}");
		return sb.ToString();
	}

	private static void AppendLines(StringBuilder sb, string lines)
	{
		foreach (string line in lines.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
		{
			sb.AppendLine(line);
		}
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
	private sealed class SchemaValidationException(string message) : Exception(message);
}
