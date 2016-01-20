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

            public static async Task<TestContext> CreateAsync(string projectLanguage = null, IEnumerable<string> metadataSources = null, bool includeXmlDocComments = false, string sourceWithSymbolReference = null)
            {
                projectLanguage = projectLanguage ?? LanguageNames.CSharp;
                metadataSources = metadataSources ?? SpecializedCollections.EmptyEnumerable<string>();
                metadataSources = !metadataSources.Any()
                    ? new[] { AbstractMetadataAsSourceTests.DefaultMetadataSource }
                    : metadataSources;

                var workspace = await CreateWorkspaceAsync(projectLanguage, metadataSources, includeXmlDocComments, sourceWithSymbolReference);
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

            public Task<MetadataAsSourceFile> GenerateSourceAsync(ISymbol symbol, Project project = null)
            {
                project = project ?? this.DefaultProject;

                // Generate and hold onto the result so it can be disposed of with this context
                return _metadataAsSourceService.GetGeneratedFileAsync(project, symbol);
            }

            public async Task<MetadataAsSourceFile> GenerateSourceAsync(string symbolMetadataName = null, Project project = null)
            {
                symbolMetadataName = symbolMetadataName ?? AbstractMetadataAsSourceTests.DefaultSymbolMetadataName;
                project = project ?? this.DefaultProject;

                // Get an ISymbol corresponding to the metadata name
                var compilation = await project.GetCompilationAsync();
                var diagnostics = compilation.GetDiagnostics().ToArray();
                Assert.Equal(0, diagnostics.Length);
                var symbol = await ResolveSymbolAsync(symbolMetadataName, compilation);

                // Generate and hold onto the result so it can be disposed of with this context
                var result = await _metadataAsSourceService.GetGeneratedFileAsync(project, symbol);

                return result;
            }

            private static string GetSpaceSeparatedTokens(string source)
            {
                var tokens = source.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s != string.Empty);
                return string.Join(" ", tokens);
            }

            public void VerifyResult(MetadataAsSourceFile file, string expected, bool compareTokens = true)
            {
                var actual = File.ReadAllText(file.FilePath).Trim();
                var actualSpan = file.IdentifierLocation.SourceSpan;

                if (compareTokens)
                {
                    // Compare tokens and verify location relative to the generated tokens
                    expected = GetSpaceSeparatedTokens(expected);
                    actual = GetSpaceSeparatedTokens(actual.Insert(actualSpan.Start, "[|").Insert(actualSpan.End + 2, "|]"));
                }
                else
                {
                    // Compare exact texts and verify that the location returned is exactly that
                    // indicated by expected
                    TextSpan expectedSpan;
                    MarkupTestFile.GetSpan(expected.TrimStart().TrimEnd(), out expected, out expectedSpan);
                    Assert.Equal(expectedSpan.Start, actualSpan.Start);
                    Assert.Equal(expectedSpan.End, actualSpan.End);
                }

                Assert.Equal(expected, actual);
            }

            public async Task GenerateAndVerifySourceAsync(string symbolMetadataName, string expected, bool compareTokens = true, Project project = null)
            {
                var result = await GenerateSourceAsync(symbolMetadataName, project);
                VerifyResult(result, expected, compareTokens);
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

            private static Task<TestWorkspace> CreateWorkspaceAsync(string projectLanguage, IEnumerable<string> metadataSources, bool includeXmlDocComments, string sourceWithSymbolReference)
            {
                var xmlString = string.Concat(@"
<Workspace>
    <Project Language=""", projectLanguage, @""" CommonReferences=""true"">");

                metadataSources = metadataSources ?? new[] { AbstractMetadataAsSourceTests.DefaultMetadataSource };

                foreach (var source in metadataSources)
                {
                    var metadataLanguage = DeduceLanguageString(source);

                    xmlString = string.Concat(xmlString, string.Format(@"
        <MetadataReferenceFromSource Language=""{0}"" CommonReferences=""true"" IncludeXmlDocComments=""{2}"">
            <Document FilePath=""MetadataDocument"">
{1}
            </Document>
        </MetadataReferenceFromSource>",
                        metadataLanguage,
                        SecurityElement.Escape(source),
                        includeXmlDocComments.ToString()));
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

                return TestWorkspace.CreateAsync(xmlString);
            }

            internal Document GetDocument(MetadataAsSourceFile file)
            {
                using (var reader = new StreamReader(file.FilePath))
                {
                    var textBuffer = _textBufferFactoryService.CreateTextBuffer(reader, _textBufferFactoryService.TextContentType);

                    Assert.True(_metadataAsSourceService.TryAddDocumentToWorkspace(file.FilePath, textBuffer));

                    return textBuffer.AsTextContainer().GetRelatedDocuments().Single();
                }
            }

            internal async Task<ISymbol> GetNavigationSymbolAsync()
            {
                var testDocument = _workspace.Documents.Single(d => d.FilePath == "SourceDocument");
                var document = _workspace.CurrentSolution.GetDocument(testDocument.Id);

                var syntaxRoot = await document.GetSyntaxRootAsync();
                var semanticModel = await document.GetSemanticModelAsync();
                return semanticModel.GetSymbolInfo(syntaxRoot.FindNode(testDocument.SelectedSpans.Single())).Symbol;
            }

            private class GenerationResult
            {
                public readonly MetadataAsSourceFile File;

                public GenerationResult(MetadataAsSourceFile file)
                {
                    this.File = file;
                }
            }
        }
    }
}
