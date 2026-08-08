// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.NET.ProjectData.Generators.Tests;

public sealed class DataModelSchemaGeneratorTests
{
	[Fact]
	public void DataModelSchemaGenerator_EmitsSha256SourceHashes()
	{
		CSharpCompilation compilation = CSharpCompilation.Create(
			"Microsoft.NET.ProjectData.Tasks",
			references: GetReferences(),
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		DataModelSchemaGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
			.AddAdditionalTexts([new TestAdditionalText("project-data-schema.json", """
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
				      { "name": "project", "description": "Project header." }
				    ]
				  },
				  "pathSentinels": {
				    "path": "<PATH>"
				  }
				}
				""")]);

		// Roslyn is still on xunit v2, so must CancellationToken.None instead of the TestContext's CancellationToken
		driver = driver.RunGenerators(compilation, CancellationToken.None);
		GeneratorRunResult result = Assert.Single(driver.GetRunResult().Results);

		Assert.NotEmpty(result.GeneratedSources);
		Assert.All(result.GeneratedSources, static source => Assert.Equal(SourceHashAlgorithm.Sha256, source.SourceText.ChecksumAlgorithm));
	}

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
}
