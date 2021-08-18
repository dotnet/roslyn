﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource
{
    [UseExportProvider]
    public abstract partial class AbstractMetadataAsSourceTests
    {
        internal static async Task GenerateAndVerifySourceAsync(
            string metadataSource, string symbolName, string projectLanguage, string expected, bool includeXmlDocComments = false, string languageVersion = null, string metadataLanguageVersion = null)
        {
            using var context = TestContext.Create(projectLanguage, SpecializedCollections.SingletonEnumerable(metadataSource), includeXmlDocComments, languageVersion: languageVersion, metadataLanguageVersion: metadataLanguageVersion);
            await context.GenerateAndVerifySourceAsync(symbolName, expected);
        }

        internal static async Task GenerateAndVerifySourceLineAsync(string source, string language, string expected)
        {
            using var context = TestContext.Create(language, sourceWithSymbolReference: source);
            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            var document = context.GetDocument(metadataAsSourceFile);
            var text = await document.GetTextAsync();
            var line = text.Lines.GetLineFromPosition(metadataAsSourceFile.IdentifierLocation.SourceSpan.Start);
            var lineText = line.ToString().Trim();

            Assert.Equal(expected, lineText);
        }

        internal static async Task TestNotReusedOnAssemblyDiffersAsync(string projectLanguage)
        {
            var metadataSources = new[]
            {
                @"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class D {}",
                @"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class D {}"
            };

            using var context = TestContext.Create(projectLanguage);
            var projectId = ProjectId.CreateNewId();
            var metadataProject = context.CurrentSolution
                .AddProject(projectId, "Metadata", "Metadata", LanguageNames.CSharp).GetProject(projectId)
                .AddMetadataReference(TestMetadata.Net451.mscorlib)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

            var references = new List<MetadataReference>();

            foreach (var source in metadataSources)
            {
                var newDoc = metadataProject.AddDocument("MetadataSource", source);
                metadataProject = newDoc.Project;
                references.Add(MetadataReference.CreateFromImage((await metadataProject.GetCompilationAsync()).EmitToArray()));
                metadataProject = metadataProject.RemoveDocument(newDoc.Id);
            }

            var project = context.DefaultProject.AddMetadataReference(references[0]);
            var a = await context.GenerateSourceAsync("D", project);

            project = project.RemoveMetadataReference(references[0]).AddMetadataReference(references[1]);
            var b = await context.GenerateSourceAsync("D", project);

            TestContext.VerifyDocumentNotReused(a, b);
        }

        internal static async Task TestSymbolIdMatchesMetadataAsync(string projectLanguage)
        {
            var metadataSource = @"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C { }";
            var symbolName = "C";

            using var context = TestContext.Create(projectLanguage, SpecializedCollections.SingletonEnumerable(metadataSource));
            var metadataSymbol = await context.ResolveSymbolAsync(symbolName);
            var metadataSymbolId = metadataSymbol.GetSymbolKey();
            var generatedFile = await context.GenerateSourceAsync(symbolName);
            var generatedDocument = context.GetDocument(generatedFile);
            var generatedCompilation = await generatedDocument.Project.GetCompilationAsync();
            var generatedSymbol = generatedCompilation.Assembly.GetTypeByMetadataName(symbolName);
            Assert.False(generatedSymbol.Locations.Where(loc => loc.IsInSource).IsEmpty());
            Assert.True(SymbolKey.GetComparer(ignoreCase: true, ignoreAssemblyKeys: false).Equals(metadataSymbolId, generatedSymbol.GetSymbolKey()));
        }
    }
}
