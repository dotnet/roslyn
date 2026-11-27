// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Schema;
using System.Text;
using System.Collections.Immutable;

namespace HtmlSchemaGenerator;

/// <summary>
/// Generates a static C# lookup table from html.xsd + CommonHTMLTypes.xsd + aria.xsd + html.loc.
/// The output is a single .g.cs file that provides O(1) element/attribute lookups for Razor HTML completions.
/// </summary>
internal static class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: HtmlSchemaGenerator <schemaFilesDir> <outputFile>");
            Console.Error.WriteLine("  schemaFilesDir: Path to HTML Editor SchemaFiles directory containing html.xsd");
            Console.Error.WriteLine("  outputFile: Path for the generated .g.cs file");
            return 1;
        }

        var schemaDir = args[0];
        var outputFile = args[1];

        if (!Directory.Exists(schemaDir))
        {
            Console.Error.WriteLine($"Schema directory not found: {schemaDir}");
            return 1;
        }

        var htmlXsd = Path.Combine(schemaDir, "html.xsd");
        if (!File.Exists(htmlXsd))
        {
            Console.Error.WriteLine($"html.xsd not found in: {schemaDir}");
            return 1;
        }

        try
        {
            var schema = SchemaParser.Parse(schemaDir);
            var code = CodeEmitter.Emit(schema);
            var elementInfoCode = CodeEmitter.EmitElementInfoStruct();
            var elementKindCode = CodeEmitter.EmitElementKindEnum();
            var attributeKindCode = CodeEmitter.EmitAttributeKindEnum();
            var attributeInfoCode = CodeEmitter.EmitAttributeInfoStruct();
            var htmlElementsCode = CodeEmitter.EmitHtmlElements(schema);
            var sharedValueGroupsCode = CodeEmitter.EmitSharedData(schema);
            var uniqueValueGroupsCode = CodeEmitter.EmitUniqueValueGroups(schema);
            var sharedAttrsCode = CodeEmitter.EmitSharedAttrs(schema);
            var uniqueAttrsCode = CodeEmitter.EmitUniqueAttrs(schema);
            var elementGroupsSharedCode = CodeEmitter.EmitElementGroupsShared(schema);
            var elementGroupsUniqueCode = CodeEmitter.EmitElementGroupsUnique(schema);
            var attrGroupsSharedCode = CodeEmitter.EmitAttributeGroupsShared(schema);
            var attrGroupsUniqueCode = CodeEmitter.EmitAttributeGroupsUnique(schema);

            outputFile = Path.GetFullPath(outputFile);
            var outputDir = Path.GetDirectoryName(outputFile)!;
            Directory.CreateDirectory(outputDir);

            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            File.WriteAllText(outputFile, code, utf8);

            var elementInfoFile = Path.Combine(outputDir!, "HtmlElementInfo.g.cs");
            File.WriteAllText(elementInfoFile, elementInfoCode, utf8);

            var elementKindFile = Path.Combine(outputDir!, "HtmlElementKind.g.cs");
            File.WriteAllText(elementKindFile, elementKindCode, utf8);

            var attributeInfoFile = Path.Combine(outputDir!, "HtmlAttributeInfo.g.cs");
            File.WriteAllText(attributeInfoFile, attributeInfoCode, utf8);

            var attributeKindFile = Path.Combine(outputDir!, "HtmlAttributeKind.g.cs");
            File.WriteAllText(attributeKindFile, attributeKindCode, utf8);

            var htmlElementsFile = Path.Combine(outputDir!, "HtmlElements.All.g.cs");
            File.WriteAllText(htmlElementsFile, htmlElementsCode, utf8);

            var sharedValueGroupsFile = Path.Combine(outputDir!, "HtmlAttributeValueGroups.Shared.g.cs");
            File.WriteAllText(sharedValueGroupsFile, sharedValueGroupsCode, utf8);

            var uniqueValueGroupsFile = Path.Combine(outputDir!, "HtmlAttributeValueGroups.Unique.g.cs");
            File.WriteAllText(uniqueValueGroupsFile, uniqueValueGroupsCode, utf8);

            var sharedAttrsFile = Path.Combine(outputDir!, "HtmlAttributes.Shared.g.cs");
            File.WriteAllText(sharedAttrsFile, sharedAttrsCode, utf8);

            var uniqueAttrsFile = Path.Combine(outputDir!, "HtmlAttributes.Unique.g.cs");
            File.WriteAllText(uniqueAttrsFile, uniqueAttrsCode, utf8);

            var elementGroupsSharedFile = Path.Combine(outputDir!, "HtmlElementGroups.Shared.g.cs");
            File.WriteAllText(elementGroupsSharedFile, elementGroupsSharedCode, utf8);

            var elementGroupsUniqueFile = Path.Combine(outputDir!, "HtmlElementGroups.Unique.g.cs");
            File.WriteAllText(elementGroupsUniqueFile, elementGroupsUniqueCode, utf8);

            var attrGroupsSharedFile = Path.Combine(outputDir!, "HtmlAttributeGroups.Shared.g.cs");
            File.WriteAllText(attrGroupsSharedFile, attrGroupsSharedCode, utf8);

            var attrGroupsUniqueFile = Path.Combine(outputDir!, "HtmlAttributeGroups.Unique.g.cs");
            File.WriteAllText(attrGroupsUniqueFile, attrGroupsUniqueCode, utf8);

            // Emit resource file for localizable descriptions
            var resxContent = CodeEmitter.EmitDescriptionsResx(schema);
            // Resources dir is at the project root level (sibling of Completion/)
            var projectDir = Path.GetFullPath(Path.Combine(outputDir!, "..", "..", ".."));
            var resourcesDir = Path.Combine(projectDir, "Resources");
            Directory.CreateDirectory(resourcesDir);
            var resxFile = Path.Combine(resourcesDir, "HtmlDescriptions.resx");
            // Normalize to CRLF so the .resx file has consistent line endings for git,
            // even though description values may contain embedded newlines from the schema.
            resxContent = resxContent.Replace("\r\n", "\n").Replace("\n", "\r\n");
            File.WriteAllText(resxFile, resxContent, utf8);

            // Emit descriptions accessor class (generated code that reads from resources)
            var accessorCode = CodeEmitter.EmitDescriptionsAccessor(schema);
            var accessorFile = Path.Combine(outputDir!, "HtmlDescriptions.g.cs");
            File.WriteAllText(accessorFile, accessorCode, utf8);

            Console.WriteLine($"Generated {outputFile}");
            Console.WriteLine($"Generated {elementInfoFile}");
            Console.WriteLine($"Generated {elementKindFile}");
            Console.WriteLine($"Generated {attributeInfoFile}");
            Console.WriteLine($"Generated {attributeKindFile}");
            Console.WriteLine($"Generated {htmlElementsFile}");
            Console.WriteLine($"Generated {sharedValueGroupsFile}");
            Console.WriteLine($"Generated {uniqueValueGroupsFile}");
            Console.WriteLine($"Generated {sharedAttrsFile}");
            Console.WriteLine($"Generated {uniqueAttrsFile}");
            Console.WriteLine($"Generated {elementGroupsSharedFile}");
            Console.WriteLine($"Generated {elementGroupsUniqueFile}");
            Console.WriteLine($"Generated {attrGroupsSharedFile}");
            Console.WriteLine($"Generated {attrGroupsUniqueFile}");
            Console.WriteLine($"Generated {resxFile}");
            Console.WriteLine($"Generated {accessorFile}");
            Console.WriteLine($"  Elements: {schema.Elements.Length}");
            Console.WriteLine($"  Global attributes: {schema.GlobalAttributes.Length}");
            Console.WriteLine($"  Descriptions: {schema.Descriptions.Count}");
            Console.WriteLine($"  Content groups: {schema.ContentGroups.Count}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
