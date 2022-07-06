// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.DecompiledSource;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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
            public readonly TestWorkspace Workspace;
            private readonly IMetadataAsSourceFileService _metadataAsSourceService;

            public static TestContext Create(
                string? projectLanguage = null,
                IEnumerable<string>? metadataSources = null,
                bool includeXmlDocComments = false,
                string? sourceWithSymbolReference = null,
                string? languageVersion = null,
                string? metadataLanguageVersion = null)
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
                Workspace = workspace;
                _metadataAsSourceService = Workspace.GetService<IMetadataAsSourceFileService>();
            }

            public Solution CurrentSolution
            {
                get { return Workspace.CurrentSolution; }
            }

            public Project DefaultProject
            {
                get { return this.CurrentSolution.Projects.First(); }
            }

            public Task<MetadataAsSourceFile> GenerateSourceAsync(ISymbol symbol, Project? project = null, bool signaturesOnly = true)
            {
                project ??= this.DefaultProject;
                Contract.ThrowIfNull(symbol);

                // Generate and hold onto the result so it can be disposed of with this context
                return _metadataAsSourceService.GetGeneratedFileAsync(project, symbol, signaturesOnly, MetadataAsSourceOptions.Default);
            }

            public async Task<MetadataAsSourceFile> GenerateSourceAsync(string? symbolMetadataName = null, Project? project = null, bool signaturesOnly = true)
            {
                symbolMetadataName ??= AbstractMetadataAsSourceTests.DefaultSymbolMetadataName;
                project ??= this.DefaultProject;

                // Get an ISymbol corresponding to the metadata name
                var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);
                var diagnostics = compilation.GetDiagnostics().ToArray();
                Assert.Equal(0, diagnostics.Length);
                var symbol = await ResolveSymbolAsync(symbolMetadataName, compilation);
                Contract.ThrowIfNull(symbol);

                if (!signaturesOnly)
                {
                    foreach (var reference in compilation.References)
                    {
                        if (AssemblyResolver.TestAccessor.ContainsInMemoryImage(reference))
                        {
                            continue;
                        }

                        if (reference is PortableExecutableReference portableExecutable)
                        {
                            Assert.True(File.Exists(portableExecutable.FilePath), $"'{portableExecutable.FilePath}' does not exist for reference '{portableExecutable.Display}'");
                            Assert.True(Path.IsPathRooted(portableExecutable.FilePath), $"'{portableExecutable.FilePath}' is not a fully-qualified file name");
                        }
                        else
                        {
                            Assert.True(File.Exists(reference.Display), $"'{reference.Display}' does not exist");
                            Assert.True(Path.IsPathRooted(reference.Display), $"'{reference.Display}' is not a fully-qualified file name");
                        }
                    }
                }

                // Generate and hold onto the result so it can be disposed of with this context
                var result = await _metadataAsSourceService.GetGeneratedFileAsync(project, symbol, signaturesOnly, MetadataAsSourceOptions.Default);

                return result;
            }

            public static void VerifyResult(MetadataAsSourceFile file, string expected)
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

            public async Task GenerateAndVerifySourceAsync(string symbolMetadataName, string expected, Project? project = null, bool signaturesOnly = true)
            {
                var result = await GenerateSourceAsync(symbolMetadataName, project, signaturesOnly);
                VerifyResult(result, expected);
            }

            public static void VerifyDocumentReused(MetadataAsSourceFile a, MetadataAsSourceFile b)
                => Assert.Same(a.FilePath, b.FilePath);

            public static void VerifyDocumentNotReused(MetadataAsSourceFile a, MetadataAsSourceFile b)
                => Assert.NotSame(a.FilePath, b.FilePath);

            public void Dispose()
            {
                try
                {
                    _metadataAsSourceService.CleanupGeneratedFiles();
                }
                finally
                {
                    Workspace.Dispose();
                }
            }

            public async Task<ISymbol?> ResolveSymbolAsync(string symbolMetadataName, Compilation? compilation = null)
            {
                if (compilation == null)
                {
                    compilation = await this.DefaultProject.GetRequiredCompilationAsync(CancellationToken.None);
                    var diagnostics = compilation.GetDiagnostics().ToArray();
                    Assert.Equal(0, diagnostics.Length);
                }

                foreach (var reference in compilation.References)
                {
                    var assemblySymbol = (IAssemblySymbol?)compilation.GetAssemblyOrModuleSymbol(reference);
                    Contract.ThrowIfNull(assemblySymbol);

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
                string projectLanguage,
                IEnumerable<string>? metadataSources,
                bool includeXmlDocComments,
                string? sourceWithSymbolReference,
                string? languageVersion,
                string? metadataLanguageVersion)
            {
                var languageVersionAttribute = languageVersion is null ? "" : $@" LanguageVersion=""{languageVersion}""";

                var xmlString = string.Concat(@"
<Workspace>
    <Project Language=""", projectLanguage, @""" CommonReferences=""true"" ReferencesOnDisk=""true""", languageVersionAttribute);

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

                // We construct our own composition here because we only want the decompilation metadata as source provider
                // to be available.
                var composition = EditorTestCompositions.EditorFeatures
                    .WithExcludedPartTypes(ImmutableHashSet.Create(typeof(IMetadataAsSourceFileProvider)))
                    .AddParts(typeof(DecompilationMetadataAsSourceFileProvider));

                return TestWorkspace.Create(xmlString, composition: composition);
            }

            internal Document GetDocument(MetadataAsSourceFile file)
            {
                using var reader = File.OpenRead(file.FilePath);
                var stringText = EncodedStringText.Create(reader);

                Assert.True(_metadataAsSourceService.TryAddDocumentToWorkspace(file.FilePath, stringText.Container));

                return stringText.Container.GetRelatedDocuments().Single();
            }

            internal async Task<ISymbol> GetNavigationSymbolAsync()
            {
                var testDocument = Workspace.Documents.Single(d => d.FilePath == "SourceDocument");
                var document = Workspace.CurrentSolution.GetRequiredDocument(testDocument.Id);

                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
                var semanticModel = await document.GetRequiredSemanticModelAsync(CancellationToken.None);
                var symbol = semanticModel.GetSymbolInfo(syntaxRoot.FindNode(testDocument.SelectedSpans.Single())).Symbol;
                Contract.ThrowIfNull(symbol);
                return symbol;
            }
        }
    }
}
