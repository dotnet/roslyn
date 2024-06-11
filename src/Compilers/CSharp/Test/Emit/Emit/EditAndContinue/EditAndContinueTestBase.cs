// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    using static EditAndContinueTestUtilities;

    public abstract class EditAndContinueTestBase : EmitMetadataTestBase
    {
        // PDB reader can only be accessed from a single thread, so avoid concurrent compilation:
        internal static readonly CSharpCompilationOptions ComSafeDebugDll = TestOptions.DebugDll.WithConcurrentBuild(false);

        internal static readonly Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> EmptyLocalsProvider = handle => default(EditAndContinueMethodDebugInformation);

        internal static string Visualize(ModuleMetadata baseline, params PinnedMetadata[] deltas)
        {
            var result = new StringWriter();
            new MetadataVisualizer(new[] { baseline.MetadataReader }.Concat(deltas.Select(d => d.Reader)).ToArray(), result).VisualizeAllGenerations();
            return result.ToString();
        }

        internal static SourceWithMarkedNodes MarkedSource(string markedSource, string fileName = "", CSharpParseOptions options = null, bool removeTags = false)
            => new SourceWithMarkedNodes(
                WithWindowsLineBreaks(markedSource),
                parser: s => Parse(s, fileName, options ?? TestOptions.Regular.WithNoRefSafetyRulesAttribute()),
                getSyntaxKind: s => (int)(SyntaxKind)typeof(SyntaxKind).GetField(s).GetValue(null),
                removeTags);

        internal static Func<SyntaxNode, SyntaxNode> GetSyntaxMapFromMarkers(SourceWithMarkedNodes source0, SourceWithMarkedNodes source1)
            => SourceWithMarkedNodes.GetSyntaxMap(source0, source1);

        internal static ImmutableArray<SyntaxNode> GetAllLocals(MethodSymbol method)
        {
            var sourceMethod = method as SourceMemberMethodSymbol;
            if (sourceMethod == null)
            {
                return ImmutableArray<SyntaxNode>.Empty;
            }

            return LocalVariableDeclaratorsCollector.GetDeclarators(sourceMethod);
        }

        internal static Func<SyntaxNode, SyntaxNode> GetSyntaxMapByKind(MethodSymbol method0, params SyntaxKind[] kinds)
        {
            return newNode =>
            {
                foreach (SyntaxKind kind in kinds)
                {
                    if (newNode.IsKind(kind))
                    {
                        return method0.DeclaringSyntaxReferences.Single().SyntaxTree.GetRoot().DescendantNodes().Single(n => n.IsKind(kind));
                    }
                }

                return null;
            };
        }

        internal static Func<SyntaxNode, SyntaxNode> GetEquivalentNodesMap(MethodSymbol method1, MethodSymbol method0)
        {
            var tree1 = method1.Locations[0].SourceTree;
            var tree0 = method0.Locations[0].SourceTree;
            Assert.NotEqual(tree1, tree0);

            var locals0 = GetAllLocals(method0);
            return s =>
            {
                var s1 = s;
                Assert.Equal(s1.SyntaxTree, tree1);
                foreach (var s0 in locals0)
                {
                    if (!SyntaxFactory.AreEquivalent(s0, s1))
                    {
                        continue;
                    }
                    // Make sure the containing statements are the same.
                    var p0 = GetNearestStatement(s0);
                    var p1 = GetNearestStatement(s1);
                    if (SyntaxFactory.AreEquivalent(p0, p1))
                    {
                        return s0;
                    }
                }
                return null;
            };
        }

        internal static string GetLocalName(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)node).Identifier.ToString();
                default:
                    throw new NotImplementedException();
            }
        }

        internal static StatementSyntax GetNearestStatement(SyntaxNode node)
        {
            while (node != null)
            {
                var statement = node as StatementSyntax;
                if (statement != null)
                {
                    return statement;
                }
                node = node.Parent;
            }
            return null;
        }

        internal static SemanticEditDescription Edit(
            SemanticEditKind kind,
            Func<Compilation, ISymbol> symbolProvider,
            Func<Compilation, ISymbol> newSymbolProvider = null,
            bool preserveLocalVariables = false)
            => new(kind, symbolProvider, newSymbolProvider, preserveLocalVariables);

        internal static EditAndContinueLogEntry Row(int rowNumber, TableIndex table, EditAndContinueOperation operation)
        {
            return new EditAndContinueLogEntry(MetadataTokens.Handle(table, rowNumber), operation);
        }

        internal static EntityHandle Handle(int rowNumber, TableIndex table)
        {
            return MetadataTokens.Handle(table, rowNumber);
        }

        /// <summary>
        /// Checks that the EncLog contains specified rows.
        /// Any default values in the expected <paramref name="rows"/> are ignored to facilitate conditional code.
        /// </summary>
        internal static void CheckEncLog(MetadataReader reader, params EditAndContinueLogEntry[] rows)
        {
            AssertEx.Equal(
                rows.Where(r => r.Handle != default),
                reader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString);
        }

        /// <summary>
        /// Checks that the EncLog contains specified definition rows. References are ignored as they are usually not interesting to validate. They are emitted as needed.
        /// Any default values in the expected <paramref name="rows"/> are ignored to facilitate conditional code.
        /// </summary>
        internal static void CheckEncLogDefinitions(MetadataReader reader, params EditAndContinueLogEntry[] rows)
        {
            AssertEx.Equal(
                rows.Where(r => r.Handle != default),
                reader.GetEditAndContinueLogEntries().Where(e => IsDefinition(e.Handle.Kind)), itemInspector: EncLogRowToString);
        }

        /// <summary>
        /// Checks that the EncMap contains specified handles.
        /// Any default values in the expected <paramref name="handles"/> are ignored to facilitate conditional code.
        /// </summary>
        internal static void CheckEncMap(MetadataReader reader, params EntityHandle[] handles)
        {
            AssertEx.Equal(
                handles.Where(h => h != default),
                reader.GetEditAndContinueMapEntries(), itemInspector: EncMapRowToString);
        }

        /// <summary>
        /// Checks that the EncMap contains specified definition handles. References are ignored as they are usually not interesting to validate. They are emitted as needed.
        /// Any default values in the expected <paramref name="handles"/> are ignored to facilitate conditional code.
        /// </summary>
        internal static void CheckEncMapDefinitions(MetadataReader reader, params EntityHandle[] handles)
        {
            AssertEx.Equal(
                handles.Where(h => h != default),
                reader.GetEditAndContinueMapEntries().Where(e => IsDefinition(e.Kind)), itemInspector: EncMapRowToString);
        }

        internal static void CheckAttributes(MetadataReader reader, params CustomAttributeRow[] rows)
        {
            AssertEx.Equal(rows, reader.GetCustomAttributeRows(), itemInspector: AttributeRowToString);
        }

        internal static void CheckNames(MetadataReader reader, IEnumerable<StringHandle> handles, params string[] expectedNames)
        {
            CheckNames(new[] { reader }, handles, expectedNames);
        }

        internal static void CheckNames(IEnumerable<MetadataReader> readers, IEnumerable<StringHandle> handles, params string[] expectedNames)
        {
            var actualNames = readers.GetStrings(handles);
            AssertEx.Equal(expectedNames, actualNames);
        }

        internal static void CheckNames(IList<MetadataReader> readers, IEnumerable<(StringHandle Namespace, StringHandle Name)> handles, params string[] expectedNames)
        {
            var actualNames = handles.Select(handlePair => string.Join(".", readers.GetString(handlePair.Namespace), readers.GetString(handlePair.Name))).ToArray();
            AssertEx.Equal(expectedNames, actualNames);
        }

        public static void CheckNames(IList<MetadataReader> readers, ImmutableArray<TypeDefinitionHandle> typeHandles, params string[] expectedNames)
            => CheckNames(readers, typeHandles, (reader, handle) => reader.GetTypeDefinition((TypeDefinitionHandle)handle).Name, handle => handle, expectedNames);

        public static void CheckNames(IList<MetadataReader> readers, ImmutableArray<MethodDefinitionHandle> methodHandles, params string[] expectedNames)
            => CheckNames(readers, methodHandles, (reader, handle) => reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name, handle => handle, expectedNames);

        private static void CheckNames<THandle>(
            IList<MetadataReader> readers,
            ImmutableArray<THandle> entityHandles,
            Func<MetadataReader, Handle, StringHandle> getName,
            Func<THandle, Handle> toHandle,
            string[] expectedNames)
        {
            var aggregator = GetAggregator(readers);

            AssertEx.Equal(expectedNames, entityHandles.Select(handle =>
            {
                var genEntityHandle = aggregator.GetGenerationHandle(toHandle(handle), out int typeGeneration);
                var nameHandle = getName(readers[typeGeneration], genEntityHandle);

                var genNameHandle = (StringHandle)aggregator.GetGenerationHandle(nameHandle, out int nameGeneration);
                return readers[nameGeneration].GetString(genNameHandle);
            }));
        }

        public static void CheckBlobValue(IList<MetadataReader> readers, BlobHandle valueHandle, byte[] expectedValue)
        {
            var aggregator = GetAggregator(readers);

            var genHandle = (BlobHandle)aggregator.GetGenerationHandle(valueHandle, out int blobGeneration);
            var attributeData = readers[blobGeneration].GetBlobBytes(genHandle);
            AssertEx.Equal(expectedValue, attributeData);
        }

        public static void CheckStringValue(IList<MetadataReader> readers, StringHandle valueHandle, string expectedValue)
        {
            var aggregator = GetAggregator(readers);

            var genHandle = (StringHandle)aggregator.GetGenerationHandle(valueHandle, out int blobGeneration);
            var attributeData = readers[blobGeneration].GetString(genHandle);
            AssertEx.Equal(expectedValue, attributeData);
        }

        public static MetadataAggregator GetAggregator(IList<MetadataReader> readers)
            => new MetadataAggregator(readers[0], readers.Skip(1).ToArray());

        internal static void SaveImages(string outputDirectory, CompilationVerifier baseline, params CompilationDifference[] diffs)
        {
            bool IsPortablePdb(ImmutableArray<byte> image) => image[0] == 'B' && image[1] == 'S' && image[2] == 'J' && image[3] == 'B';

            string baseName = baseline.Compilation.AssemblyName;
            string extSuffix = IsPortablePdb(baseline.EmittedAssemblyPdb) ? "x" : "";

            Directory.CreateDirectory(outputDirectory);

            File.WriteAllBytes(Path.Combine(outputDirectory, baseName + ".dll" + extSuffix), baseline.EmittedAssemblyData.ToArray());
            File.WriteAllBytes(Path.Combine(outputDirectory, baseName + ".pdb" + extSuffix), baseline.EmittedAssemblyPdb.ToArray());

            for (int i = 0; i < diffs.Length; i++)
            {
                File.WriteAllBytes(Path.Combine(outputDirectory, $"{baseName}.{i + 1}.metadata{extSuffix}"), diffs[i].MetadataDelta.ToArray());
                File.WriteAllBytes(Path.Combine(outputDirectory, $"{baseName}.{i + 1}.pdb{extSuffix}"), diffs[i].PdbDelta.ToArray());
            }
        }
    }

    public static class EditAndContinueTestExtensions
    {
        internal static CSharpCompilation WithSource(this CSharpCompilation compilation, CSharpTestSource newSource)
        {
            var previousParseOptions = (CSharpParseOptions)compilation.SyntaxTrees.FirstOrDefault()?.Options;
            return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(newSource.GetSyntaxTrees(previousParseOptions));
        }

        internal static CSharpCompilation WithSource(this CSharpCompilation compilation, SyntaxTree newTree)
        {
            return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(newTree);
        }

        internal static CSharpCompilation WithSource(this CSharpCompilation compilation, SyntaxTree[] newTrees)
        {
            return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(newTrees);
        }
    }
}
