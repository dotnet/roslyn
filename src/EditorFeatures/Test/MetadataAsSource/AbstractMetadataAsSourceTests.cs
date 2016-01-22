// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource
{
    public abstract partial class AbstractMetadataAsSourceTests
    {
        internal static async Task GenerateAndVerifySourceAsync(string metadataSource, string symbolName, string projectLanguage, string expected, bool compareTokens = true, bool includeXmlDocComments = false)
        {
            using (var context = await TestContext.CreateAsync(projectLanguage, SpecializedCollections.SingletonEnumerable(metadataSource), includeXmlDocComments))
            {
                await context.GenerateAndVerifySourceAsync(symbolName, expected, compareTokens);
            }
        }

        internal static async Task TestNotReusedOnAssemblyDiffersAsync(string projectLanguage)
        {
            var metadataSources = new[]
            {
                @"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class D {}",
                @"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class D {}"
            };

            using (var context = await TestContext.CreateAsync(projectLanguage))
            {
                var projectId = ProjectId.CreateNewId();
                var metadataProject = context.CurrentSolution
                    .AddProject(projectId, "Metadata", "Metadata", LanguageNames.CSharp).GetProject(projectId)
                    .AddMetadataReference(TestReferences.NetFx.v4_0_30319.mscorlib)
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

                context.VerifyDocumentNotReused(a, b);
            }
        }

        internal static async Task TestSymbolIdMatchesMetadataAsync(string projectLanguage)
        {
            var metadataSource = @"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C { }";
            var symbolName = "C";

            using (var context = await TestContext.CreateAsync(projectLanguage, SpecializedCollections.SingletonEnumerable(metadataSource)))
            {
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
}
