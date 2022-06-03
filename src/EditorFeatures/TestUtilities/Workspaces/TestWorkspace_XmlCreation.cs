// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public partial class TestWorkspace
    {
        internal static XElement CreateWorkspaceElement(
            string language,
            CompilationOptions compilationOptions = null,
            ParseOptions parseOptions = null,
            string[] files = null,
            string[] sourceGeneratedFiles = null,
            string[] metadataReferences = null,
            string extension = null,
            bool commonReferences = true,
            bool isMarkup = true)
        {
            var documentElements = new List<XElement>();

            var index = 0;
            extension ??= (language == LanguageNames.CSharp) ? CSharpExtension : VisualBasicExtension;
            if (files != null)
            {
                foreach (var file in files)
                {
                    documentElements.Add(CreateDocumentElement(
                        file, GetDefaultTestSourceDocumentName(index++, extension), parseOptions, isMarkup));
                }
            }

            if (sourceGeneratedFiles != null)
            {
                foreach (var file in sourceGeneratedFiles)
                {
                    documentElements.Add(CreateDocumentFromSourceGeneratorElement(file, GetDefaultTestSourceDocumentName(index++, extension), parseOptions));
                }
            }

            if (metadataReferences != null)
            {
                foreach (var reference in metadataReferences)
                {
                    documentElements.Add(CreateMetadataReference(reference));
                }
            }

            var projectElement = CreateProjectElement(compilationOptions?.ModuleName ?? "Test", language, commonReferences, parseOptions, compilationOptions, documentElements);
            return CreateWorkspaceElement(projectElement);
        }

        protected static XElement CreateWorkspaceElement(
            params XElement[] projectElements)
        {
            return new XElement(WorkspaceElementName, projectElements);
        }

        protected static XElement CreateProjectElement(
            string assemblyName,
            string language,
            bool commonReferences,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions,
            params object[] elements)
        {
            return new XElement(ProjectElementName,
                new XAttribute(AssemblyNameAttributeName, assemblyName),
                new XAttribute(LanguageAttributeName, language),
                commonReferences ? new XAttribute(CommonReferencesAttributeName, true) : null,
                parseOptions == null ? null : CreateLanguageVersionAttribute(parseOptions),
                parseOptions == null ? null : CreateDocumentationModeAttribute(parseOptions),
                parseOptions == null ? null : CreateFeaturesAttribute(parseOptions),
                compilationOptions == null ? null : CreateCompilationOptionsElement(compilationOptions),
                elements);
        }

        private static XAttribute CreateLanguageVersionAttribute(ParseOptions parseOptions)
        {
            var csharpOptions = parseOptions as CSharpParseOptions;
            var vbOptions = parseOptions as VisualBasicParseOptions;
            if (csharpOptions != null)
            {
                return new XAttribute(LanguageVersionAttributeName, CodeAnalysis.CSharp.LanguageVersionFacts.ToDisplayString(csharpOptions.LanguageVersion));
            }
            else if (vbOptions != null)
            {
                return new XAttribute(LanguageVersionAttributeName, CodeAnalysis.VisualBasic.LanguageVersionFacts.ToDisplayString(vbOptions.LanguageVersion));
            }
            else
            {
                return null;
            }
        }

        private static XAttribute CreateFeaturesAttribute(ParseOptions parseOptions)
        {
            if (parseOptions.Features == null || parseOptions.Features.Count == 0)
            {
                return null;
            }

            var value = string.Join(";", parseOptions.Features.Select(p => $"{p.Key}={p.Value}"));
            return new XAttribute(FeaturesAttributeName, value);
        }

        private static XAttribute CreateDocumentationModeAttribute(ParseOptions parseOptions)
        {
            if (parseOptions == null)
            {
                return null;
            }
            else
            {
                return new XAttribute(DocumentationModeAttributeName, parseOptions.DocumentationMode);
            }
        }

        private static XElement CreateCompilationOptionsElement(CompilationOptions options)
        {
            Contract.ThrowIfFalse(options.SpecificDiagnosticOptions.IsEmpty);

            var element = new XElement(CompilationOptionsElementName);

            if (options is CodeAnalysis.CSharp.CSharpCompilationOptions csOptions)
            {
                element.SetAttributeValue(AllowUnsafeAttributeName, csOptions.AllowUnsafe);
            }
            else if (options is CodeAnalysis.VisualBasic.VisualBasicCompilationOptions vbOptions)
            {
                element.Add(vbOptions.GlobalImports.AsEnumerable().Select(i => new XElement(GlobalImportElementName, i.Name)));

                if (vbOptions.RootNamespace != null)
                {
                    element.SetAttributeValue(RootNamespaceAttributeName, vbOptions.RootNamespace);
                }
            }

            if (options.GeneralDiagnosticOption != ReportDiagnostic.Default)
            {
                element.SetAttributeValue(ReportDiagnosticAttributeName, options.GeneralDiagnosticOption);
            }

            if (options.CheckOverflow)
            {
                element.SetAttributeValue(CheckOverflowAttributeName, true);
            }

            if (options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
            {
                element.SetAttributeValue(OutputKindName, options.OutputKind);
            }

            if (options.NullableContextOptions != NullableContextOptions.Disable)
            {
                element.SetAttributeValue(NullableAttributeName, options.NullableContextOptions);
            }

            return element;
        }

        private static XElement CreateMetadataReference(string path)
            => new XElement(MetadataReferenceElementName, path);

        protected static XElement CreateDocumentElement(
            string code, string filePath, ParseOptions parseOptions = null, bool isMarkup = true)
        {
            var element = new XElement(DocumentElementName,
                new XAttribute(FilePathAttributeName, filePath),
                CreateParseOptionsElement(parseOptions),
                code.Replace("\r\n", "\n"));

            if (!isMarkup)
                element.Add(new XAttribute(MarkupAttributeName, isMarkup));

            return element;
        }

        protected static XElement CreateDocumentFromSourceGeneratorElement(string code, string hintName, ParseOptions parseOptions = null)
        {
            return new XElement(DocumentFromSourceGeneratorElementName,
                new XAttribute(FilePathAttributeName, hintName),
                CreateParseOptionsElement(parseOptions),
                code.Replace("\r\n", "\n"));
        }

        private static XElement CreateParseOptionsElement(ParseOptions parseOptions)
        {
            return parseOptions == null
                ? null
                : new XElement(ParseOptionsElementName,
                    new XAttribute(KindAttributeName, parseOptions.Kind));
        }
    }
}
