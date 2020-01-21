// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource
{
    public abstract partial class AbstractMetadataAsSourceTests
    {
        public const string DefaultMetadataSource = "public class C {}";
        public const string DefaultSymbolMetadataName = "C";

        internal class TestContext : IDisposable
        {
            private readonly TestWorkspace _workspace;
            private readonly IMetadataAsSourceFileService _metadataAsSourceService;
            private readonly ITextBufferFactoryService _textBufferFactoryService;

            public static TestContext Create(
                string projectLanguage = null,
                IEnumerable<string> metadataSources = null,
                bool includeXmlDocComments = false,
                string sourceWithSymbolReference = null,
                string languageVersion = null,
                string metadataLanguageVersion = null)
            {
                projectLanguage ??= LanguageNames.CSharp;
                metadataSources ??= SpecializedCollections.EmptyEnumerable<string>();
                metadataSources = !metadataSources.Any()
                    ? new[] { AbstractMetadataAsSourceTests.DefaultMetadataSource }
                    : metadataSources;

                var workspace = CreateWorkspace(
                    projectLanguage, metadataSources, includeXmlDocComments,
                    sourceWithSymbolReference, languageVersion, metadataLanguageVersion);
                return new TestContext(workspace);
            }

            public TestContext(TestWorkspace workspace)
            {
                _workspace = workspace;
                _metadataAsSourceService = _workspace.GetService<IMetadataAsSourceFileService>();
                _textBufferFactoryService = _workspace.GetService<ITextBufferFactoryService>();
            }

            public Solution CurrentSolution
            {
                get { return _workspace.CurrentSolution; }
            }

            public Project DefaultProject
            {
                get { return this.CurrentSolution.Projects.First(); }
            }

            public Task<MetadataAsSourceFile> GenerateSourceAsync(ISymbol symbol, Project project = null, bool allowDecompilation = false)
            {
                project ??= this.DefaultProject;

                // Generate and hold onto the result so it can be disposed of with this context
                return _metadataAsSourceService.GetGeneratedFileAsync(project, symbol, allowDecompilation);
            }

            public async Task<MetadataAsSourceFile> GenerateSourceAsync(string symbolMetadataName = null, Project project = null, bool allowDecompilation = false)
            {
                symbolMetadataName ??= AbstractMetadataAsSourceTests.DefaultSymbolMetadataName;
                project ??= this.DefaultProject;

                // Get an ISymbol corresponding to the metadata name
                var compilation = await project.GetCompilationAsync();
                var diagnostics = compilation.GetDiagnostics().ToArray();
                Assert.Equal(0, diagnostics.Length);
                var symbol = await ResolveSymbolAsync(symbolMetadataName, compilation);

                // Generate and hold onto the result so it can be disposed of with this context
                var result = await _metadataAsSourceService.GetGeneratedFileAsync(project, symbol, allowDecompilation);

                return result;
            }

            private static string GetSpaceSeparatedTokens(string source)
            {
                var tokens = source.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s != string.Empty);
                return string.Join(" ", tokens);
            }

            public void VerifyResult(MetadataAsSourceFile file, string expected)
            {
                var actual = File.ReadAllText(file.FilePath).Trim();
                var actualSpan = file.IdentifierLocation.SourceSpan;

                // Compare exact texts and verify that the location returned is exactly that
                // indicated by expected
                MarkupTestFile.GetSpan(expected, out expected, out var expectedSpan);
                AssertEx.EqualOrDiff(expected, actual);
                Assert.Equal(expectedSpan.Start, actualSpan.Start);
                Assert.Equal(expectedSpan.End, actualSpan.End);
            }

            public async Task GenerateAndVerifySourceAsync(string symbolMetadataName, string expected, Project project = null)
            {
                var result = await GenerateSourceAsync(symbolMetadataName, project);
                VerifyResult(result, expected);
            }

            public void VerifyDocumentReused(MetadataAsSourceFile a, MetadataAsSourceFile b)
            {
                Assert.Same(a.FilePath, b.FilePath);
            }

            public void VerifyDocumentNotReused(MetadataAsSourceFile a, MetadataAsSourceFile b)
            {
                Assert.NotSame(a.FilePath, b.FilePath);
            }

            public void Dispose()
            {
                try
                {
                    _metadataAsSourceService.CleanupGeneratedFiles();
                }
                finally
                {
                    _workspace.Dispose();
                }
            }

            public async Task<ISymbol> ResolveSymbolAsync(string symbolMetadataName, Compilation compilation = null)
            {
                if (compilation == null)
                {
                    compilation = await this.DefaultProject.GetCompilationAsync();
                    var diagnostics = compilation.GetDiagnostics().ToArray();
                    Assert.Equal(0, diagnostics.Length);
                }

                foreach (var reference in compilation.References)
                {
                    var assemblySymbol = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference);

                    var namedTypeSymbol = assemblySymbol.GetTypeByMetadataName(symbolMetadataName);
                    if (namedTypeSymbol != null)
                    {
                        return namedTypeSymbol;
                    }
                    else
                    {
                        // The symbol name could possibly be referring to the member of a named
                        // type.  Parse the member symbol name.
                        var lastDotIndex = symbolMetadataName.LastIndexOf('.');

                        if (lastDotIndex < 0)
                        {
                            // The symbol name is not a member name and the named type was not found
                            // in this assembly
                            continue;
                        }

                        // The member symbol name itself could contain a dot (e.g. '.ctor'), so make
                        // sure we don't cut that off
                        while (lastDotIndex > 0 && symbolMetadataName[lastDotIndex - 1] == '.')
                        {
                            --lastDotIndex;
                        }

                        var memberSymbolName = symbolMetadataName.Substring(lastDotIndex + 1);
                        var namedTypeName = symbolMetadataName.Substring(0, lastDotIndex);

                        namedTypeSymbol = assemblySymbol.GetTypeByMetadataName(namedTypeName);
                        if (namedTypeSymbol != null)
                        {
                            var memberSymbol = namedTypeSymbol.GetMembers()
                                .Where(member => member.MetadataName == memberSymbolName)
                                .FirstOrDefault();

                            if (memberSymbol != null)
                            {
                                return memberSymbol;
                            }
                        }
                    }
                }

                return null;
            }

            private static bool ContainsVisualBasicKeywords(string input)
            {
                return
                    input.Contains("Class") ||
                    input.Contains("Structure") ||
                    input.Contains("Namespace") ||
                    input.Contains("Sub") ||
                    input.Contains("Function") ||
                    input.Contains("Dim");
            }

            private static string DeduceLanguageString(string input)
            {
                return ContainsVisualBasicKeywords(input)
                    ? LanguageNames.VisualBasic : LanguageNames.CSharp;
            }

            private static TestWorkspace CreateWorkspace(
                string projectLanguage, IEnumerable<string> metadataSources,
                bool includeXmlDocComments, string sourceWithSymbolReference,
                string languageVersion, string metadataLanguageVersion)
            {
                var languageVersionAttribute = languageVersion is null ? "" : $@" LanguageVersion=""{languageVersion}""";

                var xmlString = string.Concat(@"
<Workspace>
    <Project Language=""", projectLanguage, @""" CommonReferences=""true""", languageVersionAttribute);

                xmlString += ">";

                metadataSources ??= new[] { AbstractMetadataAsSourceTests.DefaultMetadataSource };

                foreach (var source in metadataSources)
                {
                    var metadataLanguage = DeduceLanguageString(source);
                    var metadataLanguageVersionAttribute = metadataLanguageVersion is null ? "" : $@" LanguageVersion=""{metadataLanguageVersion}""";
                    xmlString = string.Concat(xmlString, $@"
        <MetadataReferenceFromSource Language=""{metadataLanguage}"" CommonReferences=""true"" {metadataLanguageVersionAttribute} IncludeXmlDocComments=""{includeXmlDocComments}"">
            <Document FilePath=""MetadataDocument"">
{SecurityElement.Escape(source)}
            </Document>
        </MetadataReferenceFromSource>");
                }

                if (sourceWithSymbolReference != null)
                {
                    xmlString = string.Concat(xmlString, string.Format(@"
        <Document FilePath=""SourceDocument"">
{0}
        </Document>",
                        sourceWithSymbolReference));
                }

                xmlString = string.Concat(xmlString, @"
    </Project>
</Workspace>");

                return TestWorkspace.Create(xmlString);
            }

            internal Document GetDocument(MetadataAsSourceFile file)
            {
                using var reader = new StreamReader(file.FilePath);
                var textBuffer = _textBufferFactoryService.CreateTextBuffer(reader, _textBufferFactoryService.TextContentType);

                Assert.True(_metadataAsSourceService.TryAddDocumentToWorkspace(file.FilePath, textBuffer));

                return textBuffer.AsTextContainer().GetRelatedDocuments().Single();
            }

            internal async Task<ISymbol> GetNavigationSymbolAsync()
            {
                var testDocument = _workspace.Documents.Single(d => d.FilePath == "SourceDocument");
                var document = _workspace.CurrentSolution.GetDocument(testDocument.Id);

                var syntaxRoot = await document.GetSyntaxRootAsync();
                var semanticModel = await document.GetSemanticModelAsync();
                return semanticModel.GetSymbolInfo(syntaxRoot.FindNode(testDocument.SelectedSpans.Single())).Symbol;
            }
        }
    }
}
