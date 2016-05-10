using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        /// <summary>
        /// this gives you SymbolTreeInfo for a metadata
        /// </summary>
        public static async Task<SymbolTreeInfo> GetInfoForMetadataReferenceAsync(
            Solution solution,
            PortableExecutableReference reference,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            var metadata = reference.GetMetadata();
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

                // We don't include internals from metadata assemblies.  It's less likely that
                // a project would have IVT to it and so it helps us save on memory.  It also
                // means we can avoid loading lots and lots of obfuscated code in the case hte
                // dll was obfuscated.
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

            foreach (var moduleMetadata in GetModuleMetadata(reference.GetMetadata()))
            {
                GenerateMetadataNodes(moduleMetadata.MetadataReader, unsortedNodes);
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
            var memberLookup = LookupMetadataDefinitions(reader, globalNamespace).ToLookup(c => c.Name);

            foreach (var grouping in memberLookup)
            {
                if (UnicodeCharacterUtilities.IsValidIdentifier(grouping.Key))
                {
                    GenerateMetadataNodes(reader, grouping.Key, 0 /*index of root node*/, grouping, unsortedNodes);
                }
            }
        }

        private static void GenerateMetadataNodes(
            MetadataReader reader,
            string name,
            int parentIndex,
            IEnumerable<MetadataDefinition> symbolsWithSameName,
            List<Node> unsortedNodes)
        {
            var node = new Node(name, parentIndex);
            var nodeIndex = unsortedNodes.Count;
            unsortedNodes.Add(node);

            // Add all child members
            var membersByName = symbolsWithSameName.SelectMany(
                d => LookupMetadataDefinitions(reader, d)).ToLookup(s => s.Name);

            foreach (var grouping in membersByName)
            {
                if (UnicodeCharacterUtilities.IsValidIdentifier(grouping.Key))
                {
                    GenerateMetadataNodes(reader, grouping.Key, nodeIndex, grouping, unsortedNodes);
                }
            }
        }

        private static IEnumerable<MetadataDefinition> LookupMetadataDefinitions(
            MetadataReader reader, MetadataDefinition definition)
        {
            switch (definition.Kind)
            {
                case MetadataDefinitionKind.Namespace: return LookupMetadataDefinitions(reader, definition.Namespace);
                case MetadataDefinitionKind.Type: return LookupMetadataDefinitions(reader, definition.Type);
                default: return SpecializedCollections.EmptyEnumerable<MetadataDefinition>();
            }
        }

        private static IEnumerable<MetadataDefinition> LookupMetadataDefinitions(
            MetadataReader reader, TypeDefinition typeDefinition)
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
                        yield return new MetadataDefinition(
                            MetadataDefinitionKind.Member, reader.GetString(method.Name));
                    }
                }
            }

            foreach (var child in typeDefinition.GetNestedTypes())
            {
                var type = reader.GetTypeDefinition(child);
                if (IsPublic(type.Attributes))
                {
                    yield return MetadataDefinition.Create(reader, type);
                }
            }
        }

        private static IEnumerable<MetadataDefinition> LookupMetadataDefinitions(
            MetadataReader reader, NamespaceDefinition namespaceDefinition)
        {
            foreach (var child in namespaceDefinition.NamespaceDefinitions)
            {
                yield return MetadataDefinition.Create(reader, child);
            }

            foreach (var child in namespaceDefinition.TypeDefinitions)
            {
                var typeDefinition = reader.GetTypeDefinition(child);
                if (IsPublic(typeDefinition.Attributes))
                {
                    yield return MetadataDefinition.Create(reader, typeDefinition);
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
