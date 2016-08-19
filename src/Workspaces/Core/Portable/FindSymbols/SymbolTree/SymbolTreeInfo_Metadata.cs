// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private static string GetMetadataNameWithoutBackticks(MetadataReader reader, StringHandle name)
        {
            var typeName = reader.GetString(name);
            var index = typeName.IndexOf('`');
            typeName = index > 0 ? typeName.Substring(0, index) : typeName;
            return typeName;
        }

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
            var inheritanceMap = new OrderPreservingMultiDictionary<string, string>();

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

                GenerateMetadataNodes(reader, unsortedNodes, inheritanceMap);
            }

            return CreateSymbolTreeInfo(
                solution, 
                version, 
                reference.FilePath, 
                unsortedNodes,
                inheritanceMap);
        }

        private static void GenerateMetadataNodes(
            MetadataReader metadataReader, 
            List<Node> unsortedNodes,
            OrderPreservingMultiDictionary<string, string> inheritanceMap)
        {
            GenerateMetadataNodes(
                metadataReader, metadataReader.GetNamespaceDefinitionRoot(),
                unsortedNodes, inheritanceMap);
        }

        private static void GenerateMetadataNodes(
            MetadataReader reader, 
            NamespaceDefinition globalNamespace, 
            List<Node> unsortedNodes,
            OrderPreservingMultiDictionary<string, string> inheritanceMap)
        {
            var definitionMap = OrderPreservingMultiDictionary<string, MetadataDefinition>.GetInstance();
            try
            {
                LookupMetadataDefinitions(reader, globalNamespace, definitionMap);

                foreach (var kvp in definitionMap)
                {
                    var definitionName = kvp.Key;
                    if (UnicodeCharacterUtilities.IsValidIdentifier(definitionName))
                    {
                        GenerateMetadataNodes(
                            reader, definitionName, 0 /*index of root node*/,
                            kvp.Value, unsortedNodes, inheritanceMap);
                    }

                    PopulateInheritanceMap(reader, inheritanceMap, kvp);
                }
            }
            finally
            {
                definitionMap.Free();
            }
        }

        private static void PopulateInheritanceMap(MetadataReader reader, OrderPreservingMultiDictionary<string, string> inheritanceMap, KeyValuePair<string, OrderPreservingMultiDictionary<string, MetadataDefinition>.ValueSet> kvp)
        {
            foreach (var symbolDefinition in kvp.Value)
            {
                if (symbolDefinition.Kind == MetadataDefinitionKind.Type)
                {
                    PopulateInheritance(
                        reader, symbolDefinition.Type, inheritanceMap);
                }
            }
        }

        private static void PopulateInheritance(
            MetadataReader reader,
            TypeDefinition typeDefinition, 
            OrderPreservingMultiDictionary<string, string> inheritanceMap)
        {
            var interfaceImplHandles = typeDefinition.GetInterfaceImplementations();

            if (typeDefinition.BaseType.IsNil &&
                interfaceImplHandles.Count == 0)
            {
                return;
            }

            var typeDefinitionFullName = typeDefinition.Namespace.IsNil
                ? reader.GetString(typeDefinition.Name)
                : $"{reader.GetString(typeDefinition.Namespace)}.{reader.GetString(typeDefinition.Name)}";

            PopulateInheritance(
                reader, typeDefinitionFullName, 
                typeDefinition.BaseType, inheritanceMap);

            foreach (var interfaceImplHandle in interfaceImplHandles)
            {
                if (!interfaceImplHandle.IsNil)
                {
                    var interfaceImpl = reader.GetInterfaceImplementation(interfaceImplHandle);
                    PopulateInheritance(
                        reader, typeDefinitionFullName, 
                        interfaceImpl.Interface, inheritanceMap);
                }
            }
        }

        private static void PopulateInheritance(
            MetadataReader reader, 
            string typeDefinitionFullName,
            EntityHandle baseTypeOrInterfaceHandle,
            OrderPreservingMultiDictionary<string, string> inheritanceMap)
        {
            if (baseTypeOrInterfaceHandle.Kind == HandleKind.TypeDefinition)
            {
                var baseDefinitionHandle = (TypeDefinitionHandle)baseTypeOrInterfaceHandle;
                if (!baseDefinitionHandle.IsNil)
                {
                    var baseDefinition = reader.GetTypeDefinition(baseDefinitionHandle);
                    var baseName = GetMetadataNameWithoutBackticks(
                        reader, baseDefinition.Name);

                    inheritanceMap.Add(baseName, typeDefinitionFullName);
                }
            }
            else if (baseTypeOrInterfaceHandle.Kind == HandleKind.TypeReference)
            {
                var baseReferenceHandle = (TypeReferenceHandle)baseTypeOrInterfaceHandle;
                if (!baseReferenceHandle.IsNil)
                {
                    var baseReference = reader.GetTypeReference(baseReferenceHandle);
                    var baseName = GetMetadataNameWithoutBackticks(
                        reader, baseReference.Name);

                    inheritanceMap.Add(baseName, typeDefinitionFullName);
                }
            }
            else if (baseTypeOrInterfaceHandle.Kind == HandleKind.TypeSpecification)
            {
                var typeSpecificationHandle = (TypeSpecificationHandle)baseTypeOrInterfaceHandle;
                if (!typeSpecificationHandle.IsNil)
                {
                    var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(typeSpecificationHandle).Signature);
                    var baseName = new SignatureDecoder<string>(
                        BaseNameProvider.Instance, reader).DecodeType(ref sigReader);

                    if (baseName != "")
                    {
                        inheritanceMap.Add(baseName, typeDefinitionFullName);
                    }
                }
            }
        }

        private static void GenerateMetadataNodes(
            MetadataReader reader,
            string name,
            int parentIndex,
            OrderPreservingMultiDictionary<string, MetadataDefinition>.ValueSet definitionsWithSameName,
            List<Node> unsortedNodes,
            OrderPreservingMultiDictionary<string, string> inheritanceMap)
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
                        GenerateMetadataNodes(reader, kvp.Key, nodeIndex,
                            kvp.Value, unsortedNodes, inheritanceMap);
                    }

                    PopulateInheritanceMap(reader, inheritanceMap, kvp);
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

        /// <summary>
        /// Used to determine a simple name for a type that is referenced through
        /// a TypeSpecificationHandle.
        /// </summary>
        private class BaseNameProvider : ISignatureTypeProvider<string>
        {
            public static readonly BaseNameProvider Instance = new BaseNameProvider();

            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                return GetMetadataNameWithoutBackticks(reader, reader.GetTypeDefinition(handle).Name);
            }

            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                return GetMetadataNameWithoutBackticks(reader, reader.GetTypeReference(handle).Name);
            }

            public string GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
                return new SignatureDecoder<string>(this, reader).DecodeType(ref sigReader);
            }

            public string GetModifiedType(MetadataReader reader, bool isRequired, string modifier, string unmodifiedType) => unmodifiedType;
            public string GetPinnedType(string elementType) => elementType;
            public string GetArrayType(string elementType, ArrayShape shape) => elementType;
            public string GetByReferenceType(string elementType) => elementType;
            public string GetPointerType(string elementType) => elementType;
            public string GetSZArrayType(string elementType) => elementType;

            public string GetFunctionPointerType(MethodSignature<string> signature) => "";
            public string GetGenericMethodParameter(int index) => "";
            public string GetGenericTypeParameter(int index) => "";

            public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

            // We want the bare name as is, without any generic brackets, or backticks.
            public string GetGenericInstance(string genericType, ImmutableArray<string> typeArguments) => genericType;
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
                string typeName = GetMetadataNameWithoutBackticks(reader, definition.Name);

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
