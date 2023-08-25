// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageService
{
    internal abstract class AbstractDeclaredSymbolInfoFactoryService<
        TCompilationUnitSyntax,
        TUsingDirectiveSyntax,
        TNamespaceDeclarationSyntax,
        TTypeDeclarationSyntax,
        TEnumDeclarationSyntax,
        TMethodDeclarationSyntax,
        TMemberDeclarationSyntax,
        TNameSyntax,
        TQualifiedNameSyntax,
        TIdentifierNameSyntax> : IDeclaredSymbolInfoFactoryService
        where TCompilationUnitSyntax : SyntaxNode
        where TUsingDirectiveSyntax : SyntaxNode
        where TNamespaceDeclarationSyntax : TMemberDeclarationSyntax
        where TTypeDeclarationSyntax : TMemberDeclarationSyntax
        where TEnumDeclarationSyntax : TMemberDeclarationSyntax
        where TMethodDeclarationSyntax : TMemberDeclarationSyntax
        where TMemberDeclarationSyntax : SyntaxNode
        where TNameSyntax : SyntaxNode
        where TQualifiedNameSyntax : TNameSyntax
        where TIdentifierNameSyntax : TNameSyntax
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

        protected abstract TNameSyntax GetName(TNamespaceDeclarationSyntax node);
        protected abstract TNameSyntax GetLeft(TQualifiedNameSyntax node);
        protected abstract TNameSyntax GetRight(TQualifiedNameSyntax node);
        protected abstract SyntaxToken GetIdentifier(TIdentifierNameSyntax node);

        protected abstract string GetContainerDisplayName(TMemberDeclarationSyntax namespaceDeclaration);
        protected abstract string GetFullyQualifiedContainerName(TMemberDeclarationSyntax memberDeclaration, string rootNamespace);

        protected abstract DeclaredSymbolInfo? GetTypeDeclarationInfo(
            SyntaxNode container, TTypeDeclarationSyntax typeDeclaration, StringTable stringTable, string containerDisplayName, string fullyQualifiedContainerName);
        protected abstract DeclaredSymbolInfo GetEnumDeclarationInfo(
            SyntaxNode container, TEnumDeclarationSyntax enumDeclaration, StringTable stringTable, string containerDisplayName, string fullyQualifiedContainerName);
        protected abstract void AddMemberDeclarationInfos(
            SyntaxNode container, TMemberDeclarationSyntax memberDeclaration, StringTable stringTable, ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos, string containerDisplayName, string fullyQualifiedContainerName);
        protected abstract void AddLocalFunctionInfos(
            TMemberDeclarationSyntax memberDeclaration, StringTable stringTable, ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos, string containerDisplayName, string fullyQualifiedContainerName, CancellationToken cancellationToken);
        protected abstract void AddSynthesizedDeclaredSymbolInfos(
            SyntaxNode container, TMemberDeclarationSyntax memberDeclaration, StringTable stringTable, ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos, string containerDisplayName, string fullyQualifiedContainerName, CancellationToken cancellationToken);

        /// <summary>
        /// Get the name of the target type of specified extension method declaration. The node provided must be an
        /// extension method declaration,  i.e. calling `TryGetDeclaredSymbolInfo()` on `node` should return a
        /// `DeclaredSymbolInfo` of kind `ExtensionMethod`. If the return value is null, then it means this is a
        /// "complex" method (as described at <see cref="TopLevelSyntaxTreeIndex.ExtensionMethodInfo"/>).
        /// </summary>
        protected abstract string GetReceiverTypeName(TMethodDeclarationSyntax node);
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

        protected static void Intern(StringTable stringTable, ArrayBuilder<string> builder)
        {
            for (int i = 0, n = builder.Count; i < n; i++)
            {
                builder[i] = stringTable.Add(builder[i]);
            }
        }

        public void AddDeclaredSymbolInfos(
            ProjectState project,
            SyntaxNode root,
            ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos,
            Dictionary<string, ArrayBuilder<int>> extensionMethodInfo,
            CancellationToken cancellationToken)
        {
            var stringTable = SyntaxTreeIndex.GetStringTable(project);
            var rootNamespace = this.GetRootNamespace(project.CompilationOptions!);

            using var _1 = PooledDictionary<string, string?>.GetInstance(out var aliases);

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
            Dictionary<string, string?> aliases,
            Dictionary<string, ArrayBuilder<int>> extensionMethodInfo,
            string containerDisplayName,
            string fullyQualifiedContainerName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (memberDeclaration is TNamespaceDeclarationSyntax namespaceDeclaration)
            {
                AddNamespaceDeclaredSymbolInfos(GetName(namespaceDeclaration), fullyQualifiedContainerName);

                var innerContainerDisplayName = GetContainerDisplayName(memberDeclaration);

                foreach (var usingAlias in GetUsingAliases(namespaceDeclaration))
                {
                    if (this.TryGetAliasesFromUsingDirective(usingAlias, out var current))
                        AddAliases(aliases, current);
                }

                var innerFullyQualifiedContainerName = GetFullyQualifiedContainerName(memberDeclaration, rootNamespace);
                foreach (var child in GetChildren(namespaceDeclaration))
                {
                    AddDeclaredSymbolInfos(
                        memberDeclaration, child, stringTable, rootNamespace, declaredSymbolInfos, aliases, extensionMethodInfo,
                        innerContainerDisplayName, innerFullyQualifiedContainerName, cancellationToken);
                }
            }
            else if (memberDeclaration is TTypeDeclarationSyntax typeDeclaration)
            {
                var innerContainerDisplayName = GetContainerDisplayName(memberDeclaration);

                // Add the item for the type itself:
                declaredSymbolInfos.AddIfNotNull(GetTypeDeclarationInfo(
                    container,
                    typeDeclaration,
                    stringTable,
                    containerDisplayName,
                    fullyQualifiedContainerName));

                // Then any synthesized members in that type (for example, synthesized properties in a record):
                var innerFullyQualifiedContainerName = GetFullyQualifiedContainerName(memberDeclaration, rootNamespace);
                AddSynthesizedDeclaredSymbolInfos(
                    container,
                    memberDeclaration,
                    stringTable,
                    declaredSymbolInfos,
                    innerContainerDisplayName,
                    innerFullyQualifiedContainerName,
                    cancellationToken);

                // Then recurse into the children and add those.
                foreach (var child in GetChildren(typeDeclaration))
                {
                    AddDeclaredSymbolInfos(
                        memberDeclaration, child, stringTable, rootNamespace, declaredSymbolInfos, aliases, extensionMethodInfo,
                        innerContainerDisplayName, innerFullyQualifiedContainerName, cancellationToken);
                }
            }
            else if (memberDeclaration is TEnumDeclarationSyntax enumDeclaration)
            {
                var innerContainerDisplayName = GetContainerDisplayName(memberDeclaration);

                // Add the item for the type itself:
                declaredSymbolInfos.Add(GetEnumDeclarationInfo(
                    container,
                    enumDeclaration,
                    stringTable,
                    containerDisplayName,
                    fullyQualifiedContainerName));

                // Then recurse into the children and add those.
                var innerFullyQualifiedContainerName = GetFullyQualifiedContainerName(memberDeclaration, rootNamespace);
                foreach (var child in GetChildren(enumDeclaration))
                {
                    AddDeclaredSymbolInfos(
                        memberDeclaration, child, stringTable, rootNamespace, declaredSymbolInfos, aliases, extensionMethodInfo,
                        innerContainerDisplayName, innerFullyQualifiedContainerName, cancellationToken);
                }
            }
            else
            {
                // For anything that isn't a namespace/type/enum (generally a member), try to add the information about that
                var count = declaredSymbolInfos.Count;
                AddMemberDeclarationInfos(
                    container,
                    memberDeclaration,
                    stringTable,
                    declaredSymbolInfos,
                    containerDisplayName,
                    fullyQualifiedContainerName);

                // If the AddSingle call added an item, and that item was an extension method, then go and add the
                // information about this extension method to our 
                if (declaredSymbolInfos.Count != count &&
                    declaredSymbolInfos.Last().Kind == DeclaredSymbolInfoKind.ExtensionMethod &&
                    memberDeclaration is TMethodDeclarationSyntax methodDeclaration)
                {
                    AddExtensionMethodInfo(methodDeclaration);
                }

                AddLocalFunctionInfos(
                    memberDeclaration,
                    stringTable,
                    declaredSymbolInfos,
                    containerDisplayName,
                    fullyQualifiedContainerName,
                    cancellationToken);
            }

            return;

            // Returns the new fully-qualified-container-name built from fullyQualifiedContainerName
            // with all the pieces of 'name' added to the end of it.
            string AddNamespaceDeclaredSymbolInfos(TNameSyntax name, string fullyQualifiedContainerName)
            {
                if (name is TQualifiedNameSyntax qualifiedName)
                {
                    // Recurse down the left side of the qualified name.  Build up the new fully qualified
                    // parent name for when going down the right side.
                    var parentQualifiedContainerName = AddNamespaceDeclaredSymbolInfos(GetLeft(qualifiedName), fullyQualifiedContainerName);
                    return AddNamespaceDeclaredSymbolInfos(GetRight(qualifiedName), parentQualifiedContainerName);
                }
                else if (name is TIdentifierNameSyntax nameSyntax)
                {
                    var namespaceName = GetIdentifier(nameSyntax).ValueText;
                    declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                        stringTable,
                        namespaceName,
                        nameSuffix: null,
                        containerDisplayName: null,
                        fullyQualifiedContainerName,
                        isPartial: true,
                        hasAttributes: false,
                        DeclaredSymbolInfoKind.Namespace,
                        Accessibility.Public,
                        nameSyntax.Span,
                        inheritanceNames: ImmutableArray<string>.Empty));

                    return string.IsNullOrEmpty(fullyQualifiedContainerName)
                        ? namespaceName
                        : fullyQualifiedContainerName + "." + namespaceName;
                }
                else
                {
                    return fullyQualifiedContainerName;
                }
            }

            void AddExtensionMethodInfo(TMethodDeclarationSyntax methodDeclaration)
            {
                var declaredSymbolInfoIndex = declaredSymbolInfos.Count - 1;

                var receiverTypeName = this.GetReceiverTypeName(methodDeclaration);

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

                if (!extensionMethodInfo.TryGetValue(receiverTypeName, out var arrayBuilder))
                {
                    arrayBuilder = ArrayBuilder<int>.GetInstance();
                    extensionMethodInfo[receiverTypeName] = arrayBuilder;
                }

                arrayBuilder.Add(declaredSymbolInfoIndex);
            }
        }

        private static void AddAliases(Dictionary<string, string?> allAliases, ImmutableArray<(string aliasName, string name)> aliases)
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
