// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.NET.ProjectData.Generators.Tests;

public sealed class DataModelSchemaGeneratorTests
{
	private const string SchemaPath = "/repo/project-data-schema.json";

	[Fact]
	public void DataModelSchemaGenerator_ValidSchema_EmitsSha256SourceHashes()
	{
		GeneratorRunResult result = RunGenerator(new TestAdditionalText(SchemaPath, ValidSchema));

		Assert.Empty(result.Diagnostics);
		Assert.Equal(4, result.GeneratedSources.Length);
		Assert.All(result.GeneratedSources, static source => Assert.Equal(SourceHashAlgorithm.Sha256, source.SourceText.ChecksumAlgorithm));
		Assert.All(result.GeneratedSources, static source =>
		{
			// Generated sources must use only the current platform's newline sequence.
			string textWithoutPlatformNewLines = source.SourceText.ToString().Replace(Environment.NewLine, "");
			Assert.DoesNotContain('\r', textWithoutPlatformNewLines);
			Assert.DoesNotContain('\n', textWithoutPlatformNewLines);
		});
	}

	[Fact]
	public void DataModelSchemaGenerator_AbsentOptionalFields_UsesSemanticDefaults()
	{
		JsonObject schema = ParseValidSchema();
		JsonObject property = schema["properties"]!["required"]!.AsArray()[0]!.AsObject();
		property.Remove("description");
		JsonObject item = schema["items"]!["Compile"]!.AsObject();
		item.Remove("description");
		item.Remove("required");
		item.Remove("metadata");
		JsonObject section = schema["cacheFormat"]!["sections"]!.AsArray()[0]!.AsObject();
		section.Remove("description");
		section.Remove("itemType");

		GeneratorRunResult result = RunGenerator(new TestAdditionalText(SchemaPath, schema.ToJsonString()));

		Assert.Empty(result.Diagnostics);
		string projectItems = Assert.Single(result.GeneratedSources, static source => source.HintName == "ProjectItems.g.cs").SourceText.ToString();
		Assert.Contains("public const string Compile = \"Compile\";", projectItems);
		Assert.Contains("RequiredItemTypes = [];", projectItems);
	}

	[Fact]
	public void DataModelSchemaGenerator_DynamicValues_AreEscapedForGeneratedCSharpAndXml()
	{
		JsonObject schema = ParseValidSchema();
		schema["properties"]!["required"]!.AsArray()[0]!["description"] = """Quotes "stay"; slash / and backslash \ stay; A & B < C > D.""";
		schema["cacheFormat"]!["hashHeaderPrefix"] = "hash=\"C:\\cache\"";
		schema["cacheFormat"]!["sections"]!.AsArray()[0]!["description"] = "Section & <value>.";

		GeneratorRunResult result = RunGenerator(new TestAdditionalText(SchemaPath, schema.ToJsonString()));

		Assert.Empty(result.Diagnostics);
		string projectProperties = Assert.Single(result.GeneratedSources, static source => source.HintName == "ProjectProperties.g.cs").SourceText.ToString();
		Assert.Contains("""/// <summary>Quotes "stay"; slash / and backslash \ stay; A &amp; B &lt; C &gt; D.</summary>""", projectProperties);

		string cacheFormat = Assert.Single(result.GeneratedSources, static source => source.HintName == "CacheFormat.g.cs").SourceText.ToString();
		Assert.Contains("""public const string HashHeaderPrefix = "hash=\"C:\\cache\"";""", cacheFormat);
		Assert.Contains("/// <summary>Section &amp; &lt;value&gt;.</summary>", cacheFormat);
		Assert.DoesNotContain(
			CSharpSyntaxTree.ParseText(cacheFormat, cancellationToken: TestContext.Current.CancellationToken).GetDiagnostics(TestContext.Current.CancellationToken),
			static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
	}

	[Fact]
	public void DataModelSchemaGenerator_ControlCharacterInDynamicValue_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		schema["cacheFormat"]!["hashHeaderPrefix"] = "hash=\t";

		AssertInvalid(schema.ToJsonString(), "Field 'cacheFormat.hashHeaderPrefix' cannot contain control characters or Unicode line separators.");
	}

	[Theory]
	[InlineData("properties")]
	[InlineData("items")]
	[InlineData("cacheFormat")]
	[InlineData("pathSentinels")]
	public void DataModelSchemaGenerator_MissingRequiredTopLevelSection_ReportsDiagnostic(string section)
	{
		JsonObject schema = ParseValidSchema();
		Assert.True(schema.Remove(section));

		AssertInvalid(schema.ToJsonString(), $"Required field '{section}' is missing.");
	}

	[Fact]
	public void DataModelSchemaGenerator_MissingPropertyName_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		Assert.True(schema["properties"]!["required"]!.AsArray()[0]!.AsObject().Remove("name"));

		AssertInvalid(schema.ToJsonString(), "Required field 'properties.required[0].name' is missing.");
	}

	[Fact]
	public void DataModelSchemaGenerator_MissingSectionName_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		Assert.True(schema["cacheFormat"]!["sections"]!.AsArray()[0]!.AsObject().Remove("name"));

		AssertInvalid(schema.ToJsonString(), "Required field 'cacheFormat.sections[0].name' is missing.");
	}

	[Fact]
	public void DataModelSchemaGenerator_MissingWireFormatToken_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		Assert.True(schema["cacheFormat"]!.AsObject().Remove("versionHeader"));

		AssertInvalid(schema.ToJsonString(), "Required field 'cacheFormat.versionHeader' is missing.");
	}

	[Fact]
	public void DataModelSchemaGenerator_EmptyPathSentinelToken_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		schema["pathSentinels"]!["path"] = "";

		AssertInvalid(schema.ToJsonString(), "Field 'pathSentinels.path' must be a non-empty string.");
	}

	[Theory]
	[InlineData("properties", "Field 'properties' must be a JSON object, but found array.")]
	[InlineData("propertyList", "Field 'properties.required' must be a JSON array, but found object.")]
	[InlineData("itemRequired", "Field 'items.Compile.required' must be a JSON boolean, but found string.")]
	[InlineData("itemMetadata", "Field 'items.Compile.metadata' must be a JSON array, but found string.")]
	[InlineData("description", "Field 'properties.required[0].description' must be a JSON string, but found null.")]
	[InlineData("propertyValue", "Field 'properties.required[0].value' must be a JSON string, but found number.")]
	[InlineData("sentinel", "Field 'pathSentinels.path' must be a JSON string, but found number.")]
	public void DataModelSchemaGenerator_WrongJsonKind_ReportsDiagnostic(string mutation, string expectedDetail)
	{
		JsonObject schema = ParseValidSchema();
		switch (mutation)
		{
			case "properties":
				schema["properties"] = new JsonArray();
				break;
			case "propertyList":
				schema["properties"]!["required"] = new JsonObject();
				break;
			case "itemRequired":
				schema["items"]!["Compile"]!["required"] = "true";
				break;
			case "itemMetadata":
				schema["items"]!["Compile"]!["metadata"] = "fullPath";
				break;
			case "description":
				schema["properties"]!["required"]!.AsArray()[0]!["description"] = null;
				break;
			case "propertyValue":
				schema["properties"]!["required"]!.AsArray()[0]!["value"] = 42;
				break;
			case "sentinel":
				schema["pathSentinels"]!["path"] = 42;
				break;
			default:
				throw new InvalidOperationException($"Unknown mutation '{mutation}'.");
		}

		AssertInvalid(schema.ToJsonString(), expectedDetail);
	}

	[Fact]
	public void DataModelSchemaGenerator_DuplicatePropertyName_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		schema["properties"]!["optional"]!.AsArray()[0]!["name"] = "ProjectPath";

		AssertInvalid(
			schema.ToJsonString(),
			"Property name 'ProjectPath' is duplicated across 'properties.required' and 'properties.optional'.");
	}

	[Theory]
	[InlineData("ProjectProperties")]
	[InlineData("TypedProperties")]
	[InlineData("inner")]
	public void DataModelSchemaGenerator_PropertyNameCollidingWithGeneratedMember_ReportsDiagnostic(string name)
	{
		JsonObject schema = ParseValidSchema();
		schema["properties"]!["required"]!.AsArray()[0]!["name"] = name;

		AssertInvalid(
			schema.ToJsonString(),
			$"Field 'properties.required[0].name' cannot be '{name}' because that name is reserved by the generated ProjectProperties or TypedProperties type.");
	}

	[Fact]
	public void DataModelSchemaGenerator_ItemNameCollidingWithGeneratedType_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		JsonNode item = schema["items"]!["Compile"]!.DeepClone();
		schema["items"]!.AsObject().Remove("Compile");
		schema["items"]!["ProjectItems"] = item;

		AssertInvalid(
			schema.ToJsonString(),
			"Item name 'ProjectItems' is reserved by the generated ProjectItems type.");
	}

	[Fact]
	public void DataModelSchemaGenerator_MetadataNameCollidingWithContainingItem_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		schema["items"]!["Compile"]!["metadata"] = new JsonArray("compile");

		AssertInvalid(
			schema.ToJsonString(),
			"Field 'items.Compile.metadata[0]' produces duplicate generated member name 'Compile'.");
	}

	[Theory]
	[InlineData("sections", "Sections")]
	[InlineData("metadataBySection", "MetadataBySection")]
	public void DataModelSchemaGenerator_SectionNameCollidingWithGeneratedMember_ReportsDiagnostic(string name, string generatedName)
	{
		JsonObject schema = ParseValidSchema();
		schema["cacheFormat"]!["sections"]!.AsArray()[0]!["name"] = name;

		AssertInvalid(
			schema.ToJsonString(),
			$"Field 'cacheFormat.sections[0].name' produces duplicate generated member name '{generatedName}'.");
	}

	[Fact]
	public void DataModelSchemaGenerator_PathSentinelNameCollidingWithGeneratedType_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		schema["pathSentinels"]!["pathSentinels"] = "<COLLISION>";

		AssertInvalid(
			schema.ToJsonString(),
			"Path sentinel name 'pathSentinels' produces duplicate generated member name 'PathSentinels'.");
	}

	[Fact]
	public void DataModelSchemaGenerator_DuplicateJsonField_ReportsDiagnostic()
	{
		string schema = ValidSchema.Replace(
			"\"versionHeader\": \"version=2\",",
			"\"versionHeader\": \"version=2\", \"versionHeader\": \"version=3\",",
			StringComparison.Ordinal);

		AssertInvalid(schema, "'cacheFormat' contains duplicate field 'versionHeader'.");
	}

	[Fact]
	public void DataModelSchemaGenerator_UnknownSectionItemType_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		schema["cacheFormat"]!["sections"]!.AsArray()[0]!["itemType"] = "Unknown";

		AssertInvalid(
			schema.ToJsonString(),
			"Field 'cacheFormat.sections[0].itemType' references unknown item type 'Unknown'.");
	}

	[Fact]
	public void DataModelSchemaGenerator_MalformedVersionHeader_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		schema["cacheFormat"]!["versionHeader"] = "version=2.x";

		AssertInvalid(
			schema.ToJsonString(),
			"Field 'cacheFormat.versionHeader' must have the form 'version=<major>[.<minor>]' using non-negative integers.");
	}

	[Theory]
	[InlineData(0x2028)]
	[InlineData(0x2029)]
	public void DataModelSchemaGenerator_UnicodeLineSeparatorInWireToken_ReportsDiagnostic(int codePoint)
	{
		JsonObject schema = ParseValidSchema();
		schema["cacheFormat"]!["primaryMarker"] = "primary" + char.ConvertFromUtf32(codePoint);

		AssertInvalid(
			schema.ToJsonString(),
			"Field 'cacheFormat.primaryMarker' cannot contain control characters or Unicode line separators.");
	}

	[Fact]
	public void DataModelSchemaGenerator_UnicodeLineSeparatorInDescription_ReportsDiagnostic()
	{
		JsonObject schema = ParseValidSchema();
		schema["properties"]!["required"]!.AsArray()[0]!["description"] = "Project" + char.ConvertFromUtf32(0x2028) + "path.";

		AssertInvalid(
			schema.ToJsonString(),
			"Field 'properties.required[0].description' cannot contain control characters or Unicode line separators.");
	}

	[Fact]
	public void DataModelSchemaGenerator_InvalidJson_ReportsDiagnostic()
	{
		AssertInvalid(
			"{",
			"the file does not contain valid JSON (line 1, byte 2).");
	}

	[Fact]
	public void DataModelSchemaGenerator_UnreadableSchema_ReportsDiagnostic()
	{
		GeneratorRunResult result = RunGenerator(new ThrowingAdditionalText(SchemaPath, new IOException("read failed")));

		AssertInvalid(result, "the schema file could not be read: read failed", hasLocation: false);
	}

	[Fact]
	public void DataModelSchemaGenerator_UnexpectedReadFailure_IsNotHiddenAsSchemaDiagnostic()
	{
		GeneratorRunResult result = RunGenerator(new ThrowingAdditionalText(SchemaPath, new InvalidOperationException("programming error")));

		Assert.IsType<InvalidOperationException>(result.Exception);
		Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Id == "PDG001");
		Assert.Empty(result.GeneratedSources);
	}

	private static void AssertInvalid(string schema, string expectedDetail)
		=> AssertInvalid(RunGenerator(new TestAdditionalText(SchemaPath, schema)), expectedDetail, hasLocation: true);

	private static void AssertInvalid(GeneratorRunResult result, string expectedDetail, bool hasLocation)
	{
		Assert.Empty(result.GeneratedSources);
		Diagnostic diagnostic = Assert.Single(result.Diagnostics);
		Assert.Equal("PDG001", diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.Equal($"ProjectData schema '{SchemaPath}' is invalid: {expectedDetail}", diagnostic.GetMessage());
		if (hasLocation)
			Assert.Equal(SchemaPath, diagnostic.Location.GetLineSpan().Path);
		else
			Assert.Equal(Location.None, diagnostic.Location);
	}

	private static GeneratorRunResult RunGenerator(AdditionalText schema)
	{
		CSharpCompilation compilation = CSharpCompilation.Create(
			"Microsoft.NET.ProjectData.Tasks",
			references: GetReferences(),
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		DataModelSchemaGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
			.AddAdditionalTexts([schema])
			.RunGenerators(compilation, TestContext.Current.CancellationToken);

		return Assert.Single(driver.GetRunResult().Results);
	}

	private static JsonObject ParseValidSchema()
		=> JsonNode.Parse(ValidSchema)!.AsObject();

	private static IEnumerable<MetadataReference> GetReferences()
	{
		string trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
		return trustedPlatformAssemblies
			.Split(Path.PathSeparator)
			.Where(static path => !string.IsNullOrWhiteSpace(path))
			.Select(static path => MetadataReference.CreateFromFile(path));
	}

	private sealed class TestAdditionalText(string path, string text) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
			=> SourceText.From(text);
	}

	private sealed class ThrowingAdditionalText(string path, Exception exception) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
			=> throw exception;
	}

	private const string ValidSchema = """
		{
		  "properties": {
		    "required": [
		      { "name": "ProjectPath", "description": "Project path." }
		    ],
		    "optional": [
		      { "name": "TargetFramework", "description": "Target framework." }
		    ]
		  },
		  "items": {
		    "Compile": {
		      "description": "Compile items.",
		      "required": true,
		      "metadata": [ "fullPath" ]
		    }
		  },
		  "cacheFormat": {
		    "versionHeader": "version=2",
		    "hashHeaderPrefix": "hash=",
		    "sliceSeparator": "---",
		    "sectionOpen": "[",
		    "sectionClose": "]",
		    "commentChar": "#",
		    "projectHeaderPrefix": "project=",
		    "languagePrefix": "language=",
		    "primaryMarker": "primary",
		    "lastDtbSucceededMarker": "lastDtbSucceeded",
		    "sections": [
		      { "name": "project", "description": "Project header.", "itemType": "Compile" }
		    ]
		  },
		  "pathSentinels": {
		    "path": "<PATH>"
		  }
		}
		""";
}
