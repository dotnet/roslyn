﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractDeclaredSymbolInfoFactoryService<
        TCompilationUnitSyntax,
        TUsingDirectiveSyntax,
        TNamespaceDeclarationSyntax,
        TTypeDeclarationSyntax,
        TEnumDeclarationSyntax,
        TMemberDeclarationSyntax> : IDeclaredSymbolInfoFactoryService
        where TCompilationUnitSyntax : SyntaxNode
        where TUsingDirectiveSyntax : SyntaxNode
        where TNamespaceDeclarationSyntax : TMemberDeclarationSyntax
        where TTypeDeclarationSyntax : TMemberDeclarationSyntax
        where TEnumDeclarationSyntax : TMemberDeclarationSyntax
        where TMemberDeclarationSyntax : SyntaxNode
    {
        private static readonly ObjectPool<List<Dictionary<string, string>>> s_aliasMapListPool
            = SharedPools.Default<List<Dictionary<string, string>>>();

        // Note: these names are stored case insensitively.  That way the alias mapping works 
        // properly for VB.  It will mean that our inheritance maps may store more links in them
        // for C#.  However, that's ok.  It will be rare in practice, and all it means is that
        // we'll end up examining slightly more types (likely 0) when doing operations like 
        // Find all references.
        private static readonly ObjectPool<Dictionary<string, string>> s_aliasMapPool
            = SharedPools.StringIgnoreCaseDictionary<string>();

        protected AbstractDeclaredSymbolInfoFactoryService()
        {
        }

        protected abstract SyntaxList<TMemberDeclarationSyntax> GetChildren(TCompilationUnitSyntax node);
        protected abstract SyntaxList<TMemberDeclarationSyntax> GetChildren(TNamespaceDeclarationSyntax node);
        protected abstract SyntaxList<TMemberDeclarationSyntax> GetChildren(TTypeDeclarationSyntax node);
        protected abstract IEnumerable<TMemberDeclarationSyntax> GetChildren(TEnumDeclarationSyntax node);

        protected abstract SyntaxList<TUsingDirectiveSyntax> GetUsingAliases(TCompilationUnitSyntax node);
        protected abstract SyntaxList<TUsingDirectiveSyntax> GetUsingAliases(TNamespaceDeclarationSyntax node);

        protected abstract string GetContainerDisplayName(TMemberDeclarationSyntax namespaceDeclaration);
        protected abstract string GetFullyQualifiedContainerName(TMemberDeclarationSyntax memberDeclaration, string rootNamespace);

        protected abstract void AddDeclaredSymbolInfosWorker(
            SyntaxNode container, TMemberDeclarationSyntax memberDeclaration, StringTable stringTable, ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos, Dictionary<string, string> aliases, Dictionary<string, ArrayBuilder<int>> extensionMethodInfo, string containerDisplayName, string fullyQualifiedContainerName, CancellationToken cancellationToken);
        /// <summary>
        /// Get the name of the target type of specified extension method declaration. 
        /// The node provided must be an extension method declaration,  i.e. calling `TryGetDeclaredSymbolInfo()` 
        /// on `node` should return a `DeclaredSymbolInfo` of kind `ExtensionMethod`. 
        /// If the return value is null, then it means this is a "complex" method (as described at <see cref="SyntaxTreeIndex.ExtensionMethodInfo"/>).
        /// </summary>
        protected abstract string GetReceiverTypeName(TMemberDeclarationSyntax node);
        protected abstract bool TryGetAliasesFromUsingDirective(TUsingDirectiveSyntax node, out ImmutableArray<(string aliasName, string name)> aliases);
        protected abstract string GetRootNamespace(CompilationOptions compilationOptions);

        protected static List<Dictionary<string, string>> AllocateAliasMapList()
            => s_aliasMapListPool.Allocate();

        // We do not differentiate arrays of different kinds for simplicity.
        // e.g. int[], int[][], int[,], etc. are all represented as int[] in the index.
        protected static string CreateReceiverTypeString(string typeName, bool isArray)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return isArray
                    ? FindSymbols.Extensions.ComplexArrayReceiverTypeName
                    : FindSymbols.Extensions.ComplexReceiverTypeName;
            }
            else
            {
                return isArray
                    ? typeName + FindSymbols.Extensions.ArrayReceiverTypeNameSuffix
                    : typeName;
            }
        }

        protected static string CreateValueTupleTypeString(int elementCount)
        {
            const string ValueTupleName = "ValueTuple";
            if (elementCount == 0)
            {
                return ValueTupleName;
            }
            // A ValueTuple can have up to 8 type parameters.
            return ValueTupleName + ArityUtilities.GetMetadataAritySuffix(elementCount > 8 ? 8 : elementCount);
        }

        protected static void FreeAliasMapList(List<Dictionary<string, string>> list)
        {
            if (list != null)
            {
                foreach (var aliasMap in list)
                {
                    FreeAliasMap(aliasMap);
                }

                s_aliasMapListPool.ClearAndFree(list);
            }
        }

        protected static void FreeAliasMap(Dictionary<string, string> aliasMap)
        {
            if (aliasMap != null)
            {
                s_aliasMapPool.ClearAndFree(aliasMap);
            }
        }

        protected static Dictionary<string, string> AllocateAliasMap()
            => s_aliasMapPool.Allocate();

        protected static void AppendTokens(SyntaxNode node, StringBuilder builder)
        {
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsToken)
                {
                    builder.Append(child.AsToken().Text);
                }
                else
                {
                    AppendTokens(child.AsNode(), builder);
                }
            }
        }

        protected static void Intern(StringTable stringTable, ArrayBuilder<string> builder)
        {
            for (int i = 0, n = builder.Count; i < n; i++)
            {
                builder[i] = stringTable.Add(builder[i]);
            }
        }

        public void AddDeclaredSymbolInfos(
            Document document,
            SyntaxNode root,
            ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos,
            Dictionary<string, ArrayBuilder<int>> extensionMethodInfo,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var stringTable = SyntaxTreeIndex.GetStringTable(project);
            var rootNamespace = this.GetRootNamespace(project.CompilationOptions);

            using var _1 = PooledDictionary<string, string>.GetInstance(out var aliases);

            foreach (var usingAlias in GetUsingAliases((TCompilationUnitSyntax)root))
            {
                if (this.TryGetAliasesFromUsingDirective(usingAlias, out var current))
                    AddAliases(aliases, current);
            }

            foreach (var child in GetChildren((TCompilationUnitSyntax)root))
                AddDeclaredSymbolInfos(root, child, stringTable, rootNamespace, declaredSymbolInfos, aliases, extensionMethodInfo, "", "", cancellationToken);
        }

        private void AddDeclaredSymbolInfos(
            SyntaxNode container,
            TMemberDeclarationSyntax memberDeclaration,
            StringTable stringTable,
            string rootNamespace,
            ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos,
            Dictionary<string, string> aliases,
            Dictionary<string, ArrayBuilder<int>> extensionMethodInfo,
            string containerDisplayName,
            string fullyQualifiedContainerName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (memberDeclaration is TNamespaceDeclarationSyntax namespaceDeclaration)
            {
                var innerContainerDisplayName = GetContainerDisplayName(memberDeclaration);
                var innerFullyQualifiedContainerName = GetFullyQualifiedContainerName(memberDeclaration, rootNamespace);

                foreach (var usingAlias in GetUsingAliases(namespaceDeclaration))
                {
                    if (this.TryGetAliasesFromUsingDirective(usingAlias, out var current))
                        AddAliases(aliases, current);
                }

                foreach (var child in GetChildren(namespaceDeclaration))
                {
                    AddDeclaredSymbolInfos(
                        memberDeclaration, child, stringTable, rootNamespace, declaredSymbolInfos, aliases, extensionMethodInfo,
                        innerContainerDisplayName, innerFullyQualifiedContainerName, cancellationToken);
                }
            }
            else if (memberDeclaration is TTypeDeclarationSyntax baseTypeDeclaration)
            {
                var innerContainerDisplayName = GetContainerDisplayName(memberDeclaration);
                var innerFullyQualifiedContainerName = GetFullyQualifiedContainerName(memberDeclaration, rootNamespace);
                foreach (var child in GetChildren(baseTypeDeclaration))
                {
                    AddDeclaredSymbolInfos(
                        memberDeclaration, child, stringTable, rootNamespace, declaredSymbolInfos, aliases, extensionMethodInfo,
                        innerContainerDisplayName, innerFullyQualifiedContainerName, cancellationToken);
                }
            }
            else if (memberDeclaration is TEnumDeclarationSyntax enumDeclaration)
            {
                var innerContainerDisplayName = GetContainerDisplayName(memberDeclaration);
                var innerFullyQualifiedContainerName = GetFullyQualifiedContainerName(memberDeclaration, rootNamespace);
                foreach (var child in GetChildren(enumDeclaration))
                {
                    AddDeclaredSymbolInfos(
                        memberDeclaration, child, stringTable, rootNamespace, declaredSymbolInfos, aliases, extensionMethodInfo,
                        innerContainerDisplayName, innerFullyQualifiedContainerName, cancellationToken);
                }
            }

            AddDeclaredSymbolInfosWorker(
                container,
                memberDeclaration,
                stringTable,
                declaredSymbolInfos,
                aliases,
                extensionMethodInfo,
                containerDisplayName,
                fullyQualifiedContainerName,
                cancellationToken);
        }

        protected void AddExtensionMethodInfo(
            TMemberDeclarationSyntax node,
            Dictionary<string, string> aliases,
            int declaredSymbolInfoIndex,
            Dictionary<string, ArrayBuilder<int>> extensionMethodsInfoBuilder)
        {
            var receiverTypeName = this.GetReceiverTypeName(node);

            // Target type is an alias
            if (aliases.TryGetValue(receiverTypeName, out var originalName))
            {
                // it is an alias of multiple with identical name,
                // simply treat it as a complex method.
                if (originalName == null)
                {
                    receiverTypeName = FindSymbols.Extensions.ComplexReceiverTypeName;
                }
                else
                {
                    // replace the alias with its original name.
                    receiverTypeName = originalName;
                }
            }

            if (!extensionMethodsInfoBuilder.TryGetValue(receiverTypeName, out var arrayBuilder))
            {
                arrayBuilder = ArrayBuilder<int>.GetInstance();
                extensionMethodsInfoBuilder[receiverTypeName] = arrayBuilder;
            }

            arrayBuilder.Add(declaredSymbolInfoIndex);
        }

        private static void AddAliases(Dictionary<string, string> allAliases, ImmutableArray<(string aliasName, string name)> aliases)
        {
            foreach (var (aliasName, name) in aliases)
            {
                // In C#, it's valid to declare two alias with identical name,
                // as long as they are in different containers.
                //
                // e.g.
                //      using X = System.String;
                //      namespace N
                //      {
                //          using X = System.Int32;
                //      }
                //
                // If we detect this, we will simply treat extension methods whose
                // target type is this alias as complex method.
                if (allAliases.ContainsKey(aliasName))
                {
                    allAliases[aliasName] = null;
                }
                else
                {
                    allAliases[aliasName] = name;
                }
            }
        }
    }
}
