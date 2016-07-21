// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private static Metadata GetMetadataNoThrow(PortableExecutableReference reference)
        {
            try
            {
                return reference.GetMetadata();
            }
            catch (Exception e) when (e is BadImageFormatException || e is IOException)
            {
                return null;
            }
        }

        /// <summary>
        /// this gives you SymbolTreeInfo for a metadata
        /// </summary>
        public static async Task<SymbolTreeInfo> GetInfoForMetadataReferenceAsync(
            Solution solution,
            PortableExecutableReference reference,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            var metadata = GetMetadataNoThrow(reference);
            if (metadata == null)
            {
                return null;
            }

            // Find the lock associated with this piece of metadata.  This way only one thread is
            // computing a symbol tree info for a particular piece of metadata at a time.
            var gate = s_metadataIdToGate.GetValue(metadata.Id, s_metadataIdToGateCallback);
            using (await gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                SymbolTreeInfo info;
                if (s_metadataIdToInfo.TryGetValue(metadata.Id, out info))
                {
                    return info;
                }

                info = await LoadOrCreateMetadataSymbolTreeInfoAsync(
                    solution, reference, loadOnly, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (info == null && loadOnly)
                {
                    return null;
                }

                return s_metadataIdToInfo.GetValue(metadata.Id, _ => info);
            }
        }

        private static Task<SymbolTreeInfo> LoadOrCreateMetadataSymbolTreeInfoAsync(
            Solution solution,
            PortableExecutableReference reference,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            var filePath = reference.FilePath;
            return LoadOrCreateAsync(
                solution,
                filePath,
                loadOnly,
                create: version => CreateMetadataSymbolTreeInfo(solution, version, reference, cancellationToken),
                keySuffix: "",
                getVersion: info => info._version,
                readObject: reader => ReadSymbolTreeInfo(reader, (version, nodes) => GetSpellCheckerTask(solution, version, filePath, nodes)),
                writeObject: (w, i) => i.WriteTo(w),
                cancellationToken: cancellationToken);
        }

        private static SymbolTreeInfo CreateMetadataSymbolTreeInfo(
            Solution solution, VersionStamp version, PortableExecutableReference reference, CancellationToken cancellationToken)
        {
            var unsortedNodes = new List<Node> { new Node("", Node.RootNodeParentIndex) };

            foreach (var moduleMetadata in GetModuleMetadata(GetMetadataNoThrow(reference)))
            {
                MetadataReader reader;
                try
                {
                    reader = moduleMetadata.GetMetadataReader();
                }
                catch (BadImageFormatException)
                {
                    continue;
                }

                GenerateMetadataNodes(reader, unsortedNodes);
            }

            return CreateSymbolTreeInfo(solution, version, reference.FilePath, unsortedNodes);
        }

        private static void GenerateMetadataNodes(MetadataReader metadataReader, List<Node> unsortedNodes)
        {
            GenerateMetadataNodes(metadataReader, metadataReader.GetNamespaceDefinitionRoot(), unsortedNodes);
        }

        private static void GenerateMetadataNodes(
            MetadataReader reader, 
            NamespaceDefinition globalNamespace, 
            List<Node> unsortedNodes)
        {
            var definitionMap = OrderPreservingMultiDictionary<string, MetadataDefinition>.GetInstance();
            try
            {
                LookupMetadataDefinitions(reader, globalNamespace, definitionMap);

                foreach (var kvp in definitionMap)
                {
                    if (UnicodeCharacterUtilities.IsValidIdentifier(kvp.Key))
                    {
                        GenerateMetadataNodes(reader, kvp.Key, 0 /*index of root node*/, kvp.Value, unsortedNodes);
                    }
                }
            }
            finally
            {
                definitionMap.Free();
            }
        }

        private static void GenerateMetadataNodes(
            MetadataReader reader,
            string name,
            int parentIndex,
            OrderPreservingMultiDictionary<string, MetadataDefinition>.ValueSet definitionsWithSameName,
            List<Node> unsortedNodes)
        {
            var node = new Node(name, parentIndex);
            var nodeIndex = unsortedNodes.Count;
            unsortedNodes.Add(node);

            // Add all child members
            var definitionMap = OrderPreservingMultiDictionary<string, MetadataDefinition>.GetInstance();
            try
            {
                foreach (var definition in definitionsWithSameName)
                {
                    LookupMetadataDefinitions(reader, definition, definitionMap);
                }

                foreach (var kvp in definitionMap)
                {
                    if (UnicodeCharacterUtilities.IsValidIdentifier(kvp.Key))
                    {
                        GenerateMetadataNodes(reader, kvp.Key, nodeIndex, kvp.Value, unsortedNodes);
                    }
                }
            }
            finally
            {
                definitionMap.Free();
            }
        }

        private static void LookupMetadataDefinitions(
            MetadataReader reader, MetadataDefinition definition,
            OrderPreservingMultiDictionary<string, MetadataDefinition> definitionMap)
        {
            switch (definition.Kind)
            {
                case MetadataDefinitionKind.Namespace:
                    LookupMetadataDefinitions(reader, definition.Namespace, definitionMap);
                    break;
                case MetadataDefinitionKind.Type:
                    LookupMetadataDefinitions(reader, definition.Type, definitionMap);
                    break;
            }
        }

        private static void LookupMetadataDefinitions(
            MetadataReader reader, TypeDefinition typeDefinition,
            OrderPreservingMultiDictionary<string, MetadataDefinition> definitionMap)
        {
            // Only bother looking for extension methods in static types.
            if ((typeDefinition.Attributes & TypeAttributes.Abstract) != 0 &&
                (typeDefinition.Attributes & TypeAttributes.Sealed) != 0)
            {
                foreach (var child in typeDefinition.GetMethods())
                {
                    var method = reader.GetMethodDefinition(child);
                    if ((method.Attributes & MethodAttributes.SpecialName) != 0 ||
                        (method.Attributes & MethodAttributes.RTSpecialName) != 0)
                    {
                        continue;
                    }

                    // SymbolTreeInfo is only searched for types and extension methods.
                    // So we don't want to pull in all methods here.  As a simple approximation
                    // we just pull in methods that have attributes on them.
                    if ((method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public &&
                        (method.Attributes & MethodAttributes.Static) != 0 &&
                        method.GetCustomAttributes().Count > 0)
                    {
                        var definition = new MetadataDefinition(
                            MetadataDefinitionKind.Member, reader.GetString(method.Name));

                        definitionMap.Add(definition.Name, definition);
                    }
                }
            }

            foreach (var child in typeDefinition.GetNestedTypes())
            {
                var type = reader.GetTypeDefinition(child);

                // We don't include internals from metadata assemblies.  It's less likely that
                // a project would have IVT to it and so it helps us save on memory.  It also
                // means we can avoid loading lots and lots of obfuscated code in the case the
                // dll was obfuscated.
                if (IsPublic(type.Attributes))
                {
                    var definition = MetadataDefinition.Create(reader, type);
                    definitionMap.Add(definition.Name, definition);
                }
            }
        }

        private static void LookupMetadataDefinitions(
            MetadataReader reader, NamespaceDefinition namespaceDefinition,
            OrderPreservingMultiDictionary<string, MetadataDefinition> definitionMap)
        {
            foreach (var child in namespaceDefinition.NamespaceDefinitions)
            {
                var definition = MetadataDefinition.Create(reader, child);
                definitionMap.Add(definition.Name, definition);
            }

            foreach (var child in namespaceDefinition.TypeDefinitions)
            {
                var typeDefinition = reader.GetTypeDefinition(child);
                if (IsPublic(typeDefinition.Attributes))
                {
                    var definition = MetadataDefinition.Create(reader, typeDefinition);
                    definitionMap.Add(definition.Name, definition);
                }
            }
        }

        private static bool IsPublic(TypeAttributes attributes)
        {
            var masked = attributes & TypeAttributes.VisibilityMask;
            return masked == TypeAttributes.Public || masked == TypeAttributes.NestedPublic;
        }

        private static IEnumerable<ModuleMetadata> GetModuleMetadata(Metadata metadata)
        {
            if (metadata is AssemblyMetadata)
            {
                return ((AssemblyMetadata)metadata).GetModules();
            }
            else if (metadata is ModuleMetadata)
            {
                return SpecializedCollections.SingletonEnumerable((ModuleMetadata)metadata);
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<ModuleMetadata>();
            }
        }

        private enum MetadataDefinitionKind
        {
            Namespace,
            Type,
            Member,
        }

        private struct MetadataDefinition
        {
            public string Name { get; }
            public MetadataDefinitionKind Kind { get; }

            public NamespaceDefinition Namespace { get; private set; }
            public TypeDefinition Type { get; private set; }

            public MetadataDefinition(MetadataDefinitionKind kind, string name)
                : this()
            {
                Kind = kind;
                Name = name;
            }

            public static MetadataDefinition Create(
                MetadataReader reader, NamespaceDefinitionHandle namespaceHandle)
            {
                var definition = reader.GetNamespaceDefinition(namespaceHandle);
                return new MetadataDefinition(
                    MetadataDefinitionKind.Namespace,
                    reader.GetString(definition.Name))
                {
                    Namespace = definition
                };
            }

            public static MetadataDefinition Create(
                MetadataReader reader, TypeDefinition definition)
            {
                var typeName = reader.GetString(definition.Name);
                var index = typeName.IndexOf('`');
                typeName = index > 0 ? typeName.Substring(0, index) : typeName;

                return new MetadataDefinition(
                    MetadataDefinitionKind.Type,
                    typeName)
                {
                    Type = definition
                };
            }
        }
    }
}
