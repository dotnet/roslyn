// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public abstract class EditAndContinueTestBase : EmitMetadataTestBase
    {
        // PDB reader can only be accessed from a single thread, so avoid concurrent compilation:
        protected readonly CSharpCompilationOptions ComSafeDebugDll = TestOptions.DebugDll.WithConcurrentBuild(false);

        internal static readonly Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> EmptyLocalsProvider = handle => default(EditAndContinueMethodDebugInformation);

        internal static string Visualize(ModuleMetadata baseline, params PinnedMetadata[] deltas)
        {
            var result = new StringWriter();
            new MetadataVisualizer(new[] { baseline.MetadataReader }.Concat(deltas.Select(d => d.Reader)).ToArray(), result).VisualizeAllGenerations();
            return result.ToString();
        }

        internal static SourceWithMarkedNodes MarkedSource(string markedSource, string fileName = "", CSharpParseOptions options = null)
        {
            return new SourceWithMarkedNodes(markedSource, s => Parse(s, fileName, options), s => (int)(SyntaxKind)typeof(SyntaxKind).GetField(s).GetValue(null));
        }

        internal static Func<SyntaxNode, SyntaxNode> GetSyntaxMapFromMarkers(SourceWithMarkedNodes source0, SourceWithMarkedNodes source1)
        {
            return SourceWithMarkedNodes.GetSyntaxMap(source0, source1);
        }

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

        internal static EditAndContinueLogEntry Row(int rowNumber, TableIndex table, EditAndContinueOperation operation)
        {
            return new EditAndContinueLogEntry(MetadataTokens.Handle(table, rowNumber), operation);
        }

        internal static EntityHandle Handle(int rowNumber, TableIndex table)
        {
            return MetadataTokens.Handle(table, rowNumber);
        }

        internal static void CheckEncLog(MetadataReader reader, params EditAndContinueLogEntry[] rows)
        {
            AssertEx.Equal(rows, reader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString);
        }

        internal static void CheckEncLogDefinitions(MetadataReader reader, params EditAndContinueLogEntry[] rows)
        {
            AssertEx.Equal(rows, reader.GetEditAndContinueLogEntries().Where(IsDefinition), itemInspector: EncLogRowToString);
        }

        private static bool IsDefinition(EditAndContinueLogEntry entry)
        {
            TableIndex index;
            Assert.True(MetadataTokens.TryGetTableIndex(entry.Handle.Kind, out index));

            switch (index)
            {
                case TableIndex.MethodDef:
                case TableIndex.Field:
                case TableIndex.Constant:
                case TableIndex.GenericParam:
                case TableIndex.GenericParamConstraint:
                case TableIndex.Event:
                case TableIndex.CustomAttribute:
                case TableIndex.DeclSecurity:
                case TableIndex.Assembly:
                case TableIndex.MethodImpl:
                case TableIndex.Param:
                case TableIndex.Property:
                case TableIndex.TypeDef:
                case TableIndex.ExportedType:
                case TableIndex.StandAloneSig:
                case TableIndex.ClassLayout:
                case TableIndex.FieldLayout:
                case TableIndex.FieldMarshal:
                case TableIndex.File:
                case TableIndex.ImplMap:
                case TableIndex.InterfaceImpl:
                case TableIndex.ManifestResource:
                case TableIndex.MethodSemantics:
                case TableIndex.Module:
                case TableIndex.NestedClass:
                case TableIndex.EventMap:
                case TableIndex.PropertyMap:
                    return true;
            }

            return false;
        }

        internal static void CheckEncMap(MetadataReader reader, params EntityHandle[] handles)
        {
            AssertEx.Equal(handles, reader.GetEditAndContinueMapEntries(), itemInspector: EncMapRowToString);
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

        internal static string EncLogRowToString(EditAndContinueLogEntry row)
        {
            TableIndex tableIndex;
            MetadataTokens.TryGetTableIndex(row.Handle.Kind, out tableIndex);

            return string.Format(
                "Row({0}, TableIndex.{1}, EditAndContinueOperation.{2})",
                MetadataTokens.GetRowNumber(row.Handle),
                tableIndex,
                row.Operation);
        }

        internal static string EncMapRowToString(EntityHandle handle)
        {
            TableIndex tableIndex;
            MetadataTokens.TryGetTableIndex(handle.Kind, out tableIndex);

            return string.Format(
                "Handle({0}, TableIndex.{1})",
                MetadataTokens.GetRowNumber(handle),
                tableIndex);
        }

        internal static string AttributeRowToString(CustomAttributeRow row)
        {
            TableIndex parentTableIndex, constructorTableIndex;
            MetadataTokens.TryGetTableIndex(row.ParentToken.Kind, out parentTableIndex);
            MetadataTokens.TryGetTableIndex(row.ConstructorToken.Kind, out constructorTableIndex);

            return string.Format(
                "new CustomAttributeRow(Handle({0}, TableIndex.{1}), Handle({2}, TableIndex.{3}))",
                MetadataTokens.GetRowNumber(row.ParentToken),
                parentTableIndex,
                MetadataTokens.GetRowNumber(row.ConstructorToken),
                constructorTableIndex);
        }

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
        internal static CSharpCompilation WithSource(this CSharpCompilation compilation, string newSource)
        {
            return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(CSharpTestBase.Parse(newSource));
        }

        internal static CSharpCompilation WithSource(this CSharpCompilation compilation, SyntaxTree newTree)
        {
            return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(newTree);
        }
    }
}
