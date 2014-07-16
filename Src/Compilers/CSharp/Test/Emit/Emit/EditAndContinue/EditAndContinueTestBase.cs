// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public abstract class EditAndContinueTestBase : EmitMetadataTestBase
    {
        internal static readonly LocalVariableNameProvider EmptyLocalsProvider = token => ImmutableArray<string>.Empty;

        internal static ImmutableArray<SyntaxNode> GetAllLocals(MethodSymbol method)
        {
            return CSharpDefinitionMap.LocalVariableDeclaratorsCollector.GetDeclarators(method);
        }

        internal static ImmutableArray<string> GetLocalNames(MethodSymbol method)
        {
            var locals = GetAllLocals(method);
            return locals.SelectAsArray(GetLocalName);
        }

        internal static Func<SyntaxNode, SyntaxNode> GetSyntaxMapByKind(MethodSymbol method0, params SyntaxKind[] kinds)
        {
            return newNode =>
            {
                foreach (SyntaxKind kind in kinds) 
                {
                    if (newNode.CSharpKind() == kind)
                    {
                        return method0.DeclaringSyntaxReferences.Single().SyntaxTree.GetRoot().DescendantNodes().Single(n => n.CSharpKind() == kind);
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
            switch (node.CSharpKind())
            {
                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)node).Identifier.ToString();
                default:
                    throw new NotImplementedException();
            }
        }

        internal static ImmutableArray<string> GetLocalNames(CompilationTestData.MethodData methodData)
        {
            var locals = methodData.ILBuilder.LocalSlotManager.LocalsInOrder();
            return locals.SelectAsArray(l => l.Name);
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

        internal static Handle Handle(int rowNumber, TableIndex table)
        {
            return MetadataTokens.Handle(table, rowNumber);
        }

        internal static void CheckEncLog(MetadataReader reader, params EditAndContinueLogEntry[] rows)
        {
            AssertEx.Equal(rows, reader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString);
        }

        internal static void CheckEncMap(MetadataReader reader, params Handle[] handles)
        {
            AssertEx.Equal(handles, reader.GetEditAndContinueMapEntries(), itemInspector: EncMapRowToString);
        }

        internal static void CheckAttributes(MetadataReader reader, params CustomAttributeRow[] rows)
        {
            AssertEx.Equal(rows, reader.GetCustomAttributeRows(), itemInspector: AttributeRowToString);
        }

        internal static void CheckNames(MetadataReader reader, StringHandle[] handles, params string[] expectedNames)
        {
            CheckNames(new[] { reader }, handles, expectedNames);
        }

        internal static void CheckNames(MetadataReader[] readers, StringHandle[] handles, params string[] expectedNames)
        {
            var actualNames = readers.GetStrings(handles);
            AssertEx.Equal(actualNames, expectedNames);
        }

        internal static string EncLogRowToString(EditAndContinueLogEntry row)
        {
            TableIndex tableIndex;
            MetadataTokens.TryGetTableIndex(row.Handle.HandleType, out tableIndex);

            return string.Format(
                "Row({0}, TableIndex.{1}, EditAndContinueOperation.{2})",
                MetadataTokens.GetRowNumber(row.Handle),
                tableIndex,
                row.Operation);
        }

        internal static string EncMapRowToString(Handle handle)
        {
            TableIndex tableIndex;
            MetadataTokens.TryGetTableIndex(handle.HandleType, out tableIndex);

            return string.Format(
                "Handle({0}, TableIndex.{1})",
                MetadataTokens.GetRowNumber(handle),
                tableIndex);
        }

        internal static string AttributeRowToString(CustomAttributeRow row)
        {
            TableIndex parentTableIndex, constructorTableIndex;
            MetadataTokens.TryGetTableIndex(row.ParentToken.HandleType, out parentTableIndex);
            MetadataTokens.TryGetTableIndex(row.ConstructorToken.HandleType, out constructorTableIndex);

            return string.Format(
                "new CustomAttributeRow(Handle({0}, TableIndex.{1}), Handle({2}, TableIndex.{3}))",
                MetadataTokens.GetRowNumber(row.ParentToken),
                parentTableIndex,
                MetadataTokens.GetRowNumber(row.ConstructorToken),
                constructorTableIndex);
        }
    }
}
