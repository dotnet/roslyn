// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public partial class TestWorkspace
    {
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
            var csharpOptions = parseOptions as Microsoft.CodeAnalysis.CSharp.CSharpParseOptions;
            var vbOptions = parseOptions as Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions;
            if (csharpOptions != null)
            {
                return new XAttribute(LanguageVersionAttributeName, csharpOptions.LanguageVersion);
            }
            else if (vbOptions != null)
            {
                return new XAttribute(LanguageVersionAttributeName, vbOptions.LanguageVersion);
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
        {
            return new XElement(MetadataReferenceElementName, path);
        }

        private static XElement CreateProjectReference(string projectName)
        {
            return new XElement(ProjectReferenceElementName, projectName);
        }

        protected static XElement CreateDocumentElement(string code, string filePath, ParseOptions parseOptions = null)
        {
            return new XElement(DocumentElementName,
                new XAttribute(FilePathAttributeName, filePath),
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
