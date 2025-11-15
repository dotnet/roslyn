// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class SymbolTreeInfo
{
    /// <summary>
    /// Cache the symbol tree infos for assembly symbols produced from a particular <see
    /// cref="PortableExecutableReference"/>. Generating symbol trees for metadata can be expensive (in large
    /// metadata cases).  And it's common for us to have many threads to want to search the same metadata
    /// simultaneously. As such, we use an AsyncLazy to compute the value that can be shared among all callers.
    /// <para>
    /// We store this keyed off of the <see cref="Checksum"/> produced by <see cref="GetMetadataChecksum"/>.  This
    /// ensures that 
    /// </para>
    /// </summary>
    private static readonly ConditionalWeakTable<PortableExecutableReference, AsyncLazy<SymbolTreeInfo>> s_peReferenceToInfo = new();

    /// <summary>
    /// Similar to <see cref="s_peReferenceToInfo"/> except that this caches based on metadata id.  The primary
    /// difference here is that you can have the same MetadataId from two different <see
    /// cref="PortableExecutableReference"/>s, while having different checksums.  For example, if the aliases of a
    /// <see cref="PortableExecutableReference"/> are changed (see <see
    /// cref="PortableExecutableReference.WithAliases(IEnumerable{string})"/>, then it will have a different
    /// checksum, but same metadata ID.  As such, we can use this table to ensure we only do the expensive
    /// computation of the <see cref="SymbolTreeInfo"/> once per <see cref="MetadataId"/>, but we may then have to
    /// make a copy of it with a new <see cref="Checksum"/> if the checksums differ.
    /// </summary>
    private static readonly ConditionalWeakTable<MetadataId, AsyncLazy<SymbolTreeInfo>> s_metadataIdToSymbolTreeInfo = new();

    private static string GetMetadataNameWithoutBackticks(MetadataReader reader, StringHandle name)
    {
        var blobReader = reader.GetBlobReader(name);
        var backtickIndex = blobReader.IndexOf((byte)'`');
        if (backtickIndex == -1)
        {
            return reader.GetString(name);
        }

        unsafe
        {
            return MetadataStringDecoder.DefaultUTF8.GetString(
                blobReader.CurrentPointer, backtickIndex);
        }
    }

    public static MetadataId? GetMetadataIdNoThrow(PortableExecutableReference reference)
    {
        try
        {
            return reference.GetMetadataId();
        }
        catch (Exception e) when (e is BadImageFormatException or IOException)
        {
            return null;
        }
    }

    private static Metadata? GetMetadataNoThrow(PortableExecutableReference reference)
    {
        try
        {
            return reference.GetMetadata();
        }
        catch (Exception e) when (e is BadImageFormatException or IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Produces a <see cref="SymbolTreeInfo"/> for a given <see cref="PortableExecutableReference"/>.
    /// Note:  will never return null;
    /// </summary>
    /// <param name="checksum">Optional checksum for the <paramref name="reference"/> (produced by <see
    /// cref="GetMetadataChecksum"/>).  Can be provided if already computed.  If not provided it will be computed
    /// and used for the <see cref="SymbolTreeInfo"/>.</param>
    [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
    public static ValueTask<SymbolTreeInfo> GetInfoForMetadataReferenceAsync(
        Solution solution,
        PortableExecutableReference reference,
        Checksum? checksum,
        CancellationToken cancellationToken)
    {
        return GetInfoForMetadataReferenceAsync(
            solution.Services,
            SolutionKey.ToSolutionKey(solution),
            reference,
            checksum,
            cancellationToken);
    }

    /// <summary>
    /// Produces a <see cref="SymbolTreeInfo"/> for a given <see cref="PortableExecutableReference"/>.
    /// Note:  will never return null;
    /// </summary>
    /// <param name="checksum">Optional checksum for the <paramref name="reference"/> (produced by <see
    /// cref="GetMetadataChecksum"/>).  Can be provided if already computed.  If not provided it will be computed
    /// and used for the <see cref="SymbolTreeInfo"/>.</param>
    [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
    public static async ValueTask<SymbolTreeInfo> GetInfoForMetadataReferenceAsync(
        SolutionServices solutionServices,
        SolutionKey solutionKey,
        PortableExecutableReference reference,
        Checksum? checksum,
        CancellationToken cancellationToken)
    {
        checksum ??= GetMetadataChecksum(solutionServices, reference, cancellationToken);

        if (s_peReferenceToInfo.TryGetValue(reference, out var infoTask))
        {
            var info = await infoTask.GetValueAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfTrue(info.Checksum != checksum, "How could the info stored for a particular PEReference now have a different checksum?");
            return info;
        }

        return await GetInfoForMetadataReferenceSlowAsync(
            solutionServices, solutionKey, reference, checksum.Value, cancellationToken).ConfigureAwait(false);

        static async Task<SymbolTreeInfo> GetInfoForMetadataReferenceSlowAsync(
            SolutionServices services,
            SolutionKey solutionKey,
            PortableExecutableReference reference,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Important: this captured async lazy may live a long time *without* computing the final results. As
            // such, it is important that it not capture any large state.  For example, it should not hold onto a
            // Solution instance.
            //
            // this is keyed per reference, so that have unique SymbolTreeInfo's per reference with their own
            // correct checksum.  Ensuring we only compute this once per *Metadata* instance though is handled below in 
            // CreateMetadataSymbolTreeInfoAsync
            var asyncLazy = s_peReferenceToInfo.GetValue(
                reference,
                id => AsyncLazy.Create(static (arg, c) =>
                    CreateMetadataSymbolTreeInfoAsync(arg.services, arg.solutionKey, arg.reference, arg.checksum, c),
                    arg: (services, solutionKey, reference, checksum)));

            return await asyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        static async Task<SymbolTreeInfo> CreateMetadataSymbolTreeInfoAsync(
            SolutionServices services,
            SolutionKey solutionKey,
            PortableExecutableReference reference,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            var metadataId = GetMetadataIdNoThrow(reference);
            if (metadataId == null)
                return CreateEmpty(checksum);

            var asyncLazy = s_metadataIdToSymbolTreeInfo.GetValue(
                metadataId,
                metadataId => AsyncLazy.Create(static (arg, cancellationToken) =>
                        LoadOrCreateAsync(
                        arg.services,
                        arg.solutionKey,
                        arg.checksum,
                        createAsync: checksum => new ValueTask<SymbolTreeInfo>(new MetadataInfoCreator(checksum, GetMetadataNoThrow(arg.reference)).Create()),
                        keySuffix: GetMetadataKeySuffix(arg.reference),
                        cancellationToken),
                    arg: (services, solutionKey, checksum, reference)));

            var metadataIdSymbolTreeInfo = await asyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);

            // we got the info that was originally computed against this particular metadata-id.  However, the same
            // ID could be reused across different PEReferences/checksums (for example, a PEReference whose aliases
            // were changed).  As such, if this doesn't correspond to the same checksum, make a copy of this tree
            // specific to the checksum we were asked for.
            return metadataIdSymbolTreeInfo.WithChecksum(checksum);
        }
    }

    public static async Task<SymbolTreeInfo?> TryGetCachedInfoForMetadataReferenceIgnoreChecksumAsync(PortableExecutableReference reference, CancellationToken cancellationToken)
    {
        if (!s_peReferenceToInfo.TryGetValue(reference, out var infoTask))
            return null;

        return await infoTask.GetValueAsync(cancellationToken).ConfigureAwait(false);
    }

    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/33131", AllowCaptures = false)]
    public static Checksum GetMetadataChecksum(
        SolutionServices services, PortableExecutableReference reference, CancellationToken cancellationToken)
    {
        // We can reuse the index for any given reference as long as it hasn't changed.
        // So our checksum is just the checksum for the PEReference itself.
        return ChecksumCache.GetOrCreate(reference, static (reference, tuple) =>
        {
            var (services, cancellationToken) = tuple;
            var serializer = services.GetRequiredService<ISerializerService>();
            var checksum = serializer.CreateChecksum(reference, cancellationToken);

            // Include serialization format version in our checksum.  That way if the 
            // version ever changes, all persisted data won't match the current checksum
            // we expect, and we'll recompute things.
            return Checksum.Create(checksum, SerializationFormatChecksum);
        }, (services, cancellationToken));
    }

    private static string GetMetadataKeySuffix(PortableExecutableReference reference)
        => "_Metadata_" + reference.FilePath;

    /// <summary>
    /// Loads any info we have for this reference from our persistence store.  Will succeed regardless of the
    /// checksum of the <paramref name="reference"/>.  Should only be used by clients that are ok with potentially
    /// stale data.
    /// </summary>
    public static Task<SymbolTreeInfo?> LoadAnyInfoForMetadataReferenceAsync(
        Solution solution,
        PortableExecutableReference reference,
        CancellationToken cancellationToken)
    {
        return LoadAsync(
            solution.Services,
            SolutionKey.ToSolutionKey(solution),
            checksum: GetMetadataChecksum(solution.Services, reference, cancellationToken),
            checksumMustMatch: false,
            keySuffix: GetMetadataKeySuffix(reference),
            cancellationToken);
    }

    private struct MetadataInfoCreator(
        Checksum checksum, Metadata? metadata) : IDisposable
    {
        private static readonly Predicate<string> s_isNotNullOrEmpty = s => !string.IsNullOrEmpty(s);
        private static readonly ObjectPool<List<string>> s_stringListPool = SharedPools.Default<List<string>>();
        private readonly OrderPreservingMultiDictionary<string, string> _inheritanceMap = OrderPreservingMultiDictionary<string, string>.GetInstance();
        private readonly OrderPreservingMultiDictionary<MetadataNode, MetadataNode> _parentToChildren = OrderPreservingMultiDictionary<MetadataNode, MetadataNode>.GetInstance();
        private readonly MetadataNode _rootNode = MetadataNode.Allocate(name: "");

        // The set of type definitions we've read out of the current metadata reader.
        private readonly List<MetadataDefinition> _allTypeDefinitions = [];

        // Map from node represents extension member to list of possible parameter type info.
        // We can have more than one if there's multiple members with same name but different receiver type.
        // e.g.
        //
        //      public static bool AnotherExtensionMethod1(this int x);
        //      public static bool AnotherExtensionMethod1(this bool x);
        //
        private readonly MultiDictionary<MetadataNode, (ParameterTypeInfo typeInfo, bool isModernExtension)> _extensionMemberToParameterTypeInfo = [];
        private bool _containsExtensionMembers = false;

        private static ImmutableArray<ModuleMetadata> GetModuleMetadata(Metadata? metadata)
        {
            try
            {
                if (metadata is AssemblyMetadata assembly)
                {
                    return assembly.GetModules();
                }
                else if (metadata is ModuleMetadata module)
                {
                    return [module];
                }
            }
            catch (BadImageFormatException)
            {
                // Trying to get the modules of an assembly can throw.  For example, if 
                // there is an invalid public-key defined for the assembly.  See:
                // https://devdiv.visualstudio.com/DevDiv/_workitems?id=234447
            }

            return [];
        }

        internal SymbolTreeInfo Create()
        {
            foreach (var moduleMetadata in GetModuleMetadata(metadata))
            {
                try
                {
                    var metadataReader = moduleMetadata.GetMetadataReader();

                    // First, walk all the symbols from metadata, populating the parentToChilren
                    // map accordingly.
                    GenerateMetadataNodes(metadataReader);

                    // Now, once we populated the initial map, go and get all the inheritance 
                    // information for all the types in the metadata.  This may refer to 
                    // types that we haven't seen yet.  We'll add those types to the parentToChildren
                    // map accordingly.
                    PopulateInheritanceMap(metadataReader);

                    // Clear the set of type definitions we read out of this piece of metadata.
                    _allTypeDefinitions.Clear();
                }
                catch (BadImageFormatException)
                {
                    // any operation off metadata can throw BadImageFormatException
                    continue;
                }
            }

            var receiverTypeNameToExtensionMemberMap = new MultiDictionary<string, ExtensionMemberInfo>();
            var unsortedNodes = GenerateUnsortedNodes(receiverTypeNameToExtensionMemberMap);

            return CreateSymbolTreeInfo(
                checksum, unsortedNodes, _inheritanceMap, receiverTypeNameToExtensionMemberMap);
        }

        public readonly void Dispose()
        {
            // Return all the metadata nodes back to the pool so that they can be
            // used for the next PEReference we read.
            foreach (var (_, children) in _parentToChildren)
            {
                foreach (var child in children)
                    MetadataNode.Free(child);
            }

            MetadataNode.Free(_rootNode);

            _parentToChildren.Free();
            _inheritanceMap.Free();
        }

        private void GenerateMetadataNodes(MetadataReader metadataReader)
        {
            var globalNamespace = metadataReader.GetNamespaceDefinitionRoot();
            var definitionMap = OrderPreservingMultiDictionary<string, MetadataDefinition>.GetInstance();
            try
            {
                LookupMetadataDefinitions(metadataReader, globalNamespace, definitionMap);

                foreach (var (name, definitions) in definitionMap)
                    GenerateMetadataNodes(metadataReader, _rootNode, name, definitions);
            }
            finally
            {
                definitionMap.Free();
            }
        }

        private void GenerateMetadataNodes(
            MetadataReader metadataReader,
            MetadataNode parentNode,
            string nodeName,
            OrderPreservingMultiDictionary<string, MetadataDefinition>.ValueSet definitionsWithSameName)
        {
            if (!UnicodeCharacterUtilities.IsValidIdentifier(nodeName))
            {
                return;
            }

            var childNode = MetadataNode.Allocate(nodeName);
            _parentToChildren.Add(parentNode, childNode);

            // Add all child members
            var definitionMap = OrderPreservingMultiDictionary<string, MetadataDefinition>.GetInstance();
            try
            {
                foreach (var definition in definitionsWithSameName)
                {
                    if (definition.Kind == MetadataDefinitionKind.Member)
                    {
                        // We need to support having multiple members with same name but different receiver type.
                        _extensionMemberToParameterTypeInfo.Add(childNode, (definition.ReceiverTypeInfo, definition.IsModernExtension));
                    }

                    LookupMetadataDefinitions(metadataReader, definition, definitionMap);
                }

                foreach (var (name, definitions) in definitionMap)
                    GenerateMetadataNodes(metadataReader, childNode, name, definitions);
            }
            finally
            {
                definitionMap.Free();
            }
        }

        private void LookupMetadataDefinitions(
            MetadataReader metadataReader,
            MetadataDefinition definition,
            OrderPreservingMultiDictionary<string, MetadataDefinition> definitionMap)
        {
            switch (definition.Kind)
            {
                case MetadataDefinitionKind.Namespace:
                    LookupMetadataDefinitions(metadataReader, definition.Namespace, definitionMap);
                    break;
                case MetadataDefinitionKind.Type:
                    LookupMetadataDefinitions(metadataReader, definition.Type, definitionMap);
                    break;
            }
        }

        private void LookupMetadataDefinitions(
            MetadataReader metadataReader,
            TypeDefinition typeDefinition,
            OrderPreservingMultiDictionary<string, MetadataDefinition> definitionMap)
        {
            var currentTypeIsStaticClass = IsStaticClass(typeDefinition);

            // Only bother looking for extension methods in static types. Note this check means we would ignore
            // extension methods declared in assemblies compiled from VB code, since a module in VB is compiled into
            // class with "sealed" attribute but not "abstract". Although this can be addressed by checking custom
            // attributes, we believe this is not a common scenario to warrant potential perf impact.
            if (currentTypeIsStaticClass)
            {
                // Handle classic extension methods.
                _containsExtensionMembers = LoadClassicExtensionMethods() || _containsExtensionMembers;
            }

            foreach (var child in typeDefinition.GetNestedTypes())
            {
                var nestedType = metadataReader.GetTypeDefinition(child);

                // We don't include internals from metadata assemblies.  It's less likely that a project would have IVT
                // to it and so it helps us save on memory.  It also means we can avoid loading lots and lots of
                // obfuscated code in the case the dll was obfuscated.
                if (IsPublicType(nestedType))
                {
                    var definition = MetadataDefinition.Create(metadataReader, nestedType);
                    definitionMap.Add(definition.Name, definition);
                    _allTypeDefinitions.Add(definition);

                    // Look for modern extension blocks and load the members from there.
                    if (currentTypeIsStaticClass)
                        _containsExtensionMembers = LoadModernExtensionMembers(nestedType) || _containsExtensionMembers;
                }
            }

            static bool HasSpecialName(TypeDefinition typeDefinition)
                => (typeDefinition.Attributes & TypeAttributes.SpecialName) != 0 ||
                   (typeDefinition.Attributes & TypeAttributes.RTSpecialName) != 0;

            static bool IsStaticClass(TypeDefinition typeDefinition)
                => (typeDefinition.Attributes & TypeAttributes.Abstract) != 0 &&
                   (typeDefinition.Attributes & TypeAttributes.Sealed) != 0 &&
                   typeDefinition.GetGenericParameters().Count == 0;

            bool IsPublicMethod(MethodDefinitionHandle methodHandle)
            {
                if (methodHandle.IsNil)
                    return false;

                var method = metadataReader.GetMethodDefinition(methodHandle);
                return (method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
            }

            bool LoadClassicExtensionMethods()
            {
                var containsExtensionMembers = false;
                foreach (var child in typeDefinition.GetMethods())
                {
                    var method = metadataReader.GetMethodDefinition(child);

                    // SymbolTreeInfo is only searched for types and extension members. So we don't want to pull in all
                    // methods here.  As a simple approximation we just pull in methods that have attributes on them.

                    if ((method.Attributes & MethodAttributes.SpecialName) != 0 ||
                        (method.Attributes & MethodAttributes.RTSpecialName) != 0 ||
                        (method.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public ||
                        (method.Attributes & MethodAttributes.Static) == 0 ||
                        method.GetParameters().Count == 0 ||
                        method.GetCustomAttributes().Count == 0)
                    {
                        continue;
                    }

                    // Decode method signature to get the receiver type name (i.e. type name for the first parameter)
                    var blob = metadataReader.GetBlobReader(method.Signature);
                    var decoder = new SignatureDecoder<ParameterTypeInfo, object?>(ParameterTypeInfoProvider.Instance, metadataReader, genericContext: null);
                    var signature = decoder.DecodeMethodSignature(ref blob);

                    // It'd be good if we don't need to go through all parameters and make unnecessary allocations.
                    // However, this is not possible with metadata reader API right now (although it's possible by
                    // copying code from metadata reader implementation)
                    if (signature.ParameterTypes.Length == 0)
                        continue;

                    containsExtensionMembers = true;
                    var firstParameterTypeInfo = signature.ParameterTypes[0];
                    var definition = new MetadataDefinition(
                        MetadataDefinitionKind.Member,
                        metadataReader.GetString(method.Name),
                        isModernExtension: false,
                        firstParameterTypeInfo);
                    definitionMap.Add(definition.Name, definition);
                }

                return containsExtensionMembers;
            }

            bool LoadModernExtensionMembers(TypeDefinition nestedType)
            {
                var containsExtensionMembers = false;
                var extensionParameterTypeInfo = TryGetExtensionParameterTypeInfo(nestedType);
                if (extensionParameterTypeInfo is not null)
                {
                    // Ok, this is definitely an extension block.  Load all the extension members from within it.
                    foreach (var childMethod in nestedType.GetMethods())
                    {
                        var method = metadataReader.GetMethodDefinition(childMethod);
                        // Don't bother loading instance extension methods from modern extension blocks. We'll have
                        // already loaded the static classic extension method above.
                        if ((method.Attributes & MethodAttributes.SpecialName) != 0 ||
                            (method.Attributes & MethodAttributes.RTSpecialName) != 0 ||
                            (method.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public ||
                            (method.Attributes & MethodAttributes.Static) == 0)
                        {
                            continue;
                        }

                        var definition = new MetadataDefinition(
                            MetadataDefinitionKind.Member,
                            metadataReader.GetString(method.Name),
                            isModernExtension: true,
                            extensionParameterTypeInfo.Value);
                        definitionMap.Add(definition.Name, definition);
                    }

                    foreach (var childProperty in nestedType.GetProperties())
                    {
                        var property = metadataReader.GetPropertyDefinition(childProperty);
                        if ((property.Attributes & PropertyAttributes.SpecialName) != 0 ||
                            (property.Attributes & PropertyAttributes.RTSpecialName) != 0)
                        {
                            continue;
                        }

                        var accessors = property.GetAccessors();

                        if (!IsPublicMethod(accessors.Getter) && !IsPublicMethod(accessors.Setter))
                            continue;

                        containsExtensionMembers = true;
                        var definition = new MetadataDefinition(
                            MetadataDefinitionKind.Member,
                            metadataReader.GetString(property.Name),
                            isModernExtension: true,
                            extensionParameterTypeInfo.Value);
                        definitionMap.Add(definition.Name, definition);
                    }
                }

                return containsExtensionMembers;
            }

            ParameterTypeInfo? TryGetExtensionParameterTypeInfo(TypeDefinition typeDefinition)
            {
                // There will be two nested types within the outer normal static class.  It should look like:
                //
                //  static class NormalClass
                //  {
                //      [SpecialName, Extension] public sealed class <G>$34505F560D9EACF86A87F3ED1F85E448
                //      {
                //          [SpecialName] public static class <M>$97B1E6A5993F490204BC5DC367191ECC
                //          {
                //              // This is used to determine the extension block parameter type
                //              [CompilerGenerated] public static <Extension>$(parameter) { }
                //          }
                //
                //          public void ExtensionMethod() { }
                //          public int ExtensionProp => ...
                //      }
                //  }
                //
                // Note: to keep things simple, we don't check actual attributes.  We just check enough to give us
                // confidencce that we found the right thing.  (So same modifiers, has or does not have a special name,
                // has attributes, etc).

                if ((typeDefinition.Attributes & TypeAttributes.Sealed) == 0 ||
                    !IsPublicType(typeDefinition) ||
                    !HasSpecialName(typeDefinition) ||
                    typeDefinition.GetCustomAttributes().Count == 0)
                {
                    return null;
                }

                // Ok, we've seen the first interesting inner type.  Now look for the one inside of that
                // that contains the maker method.
                foreach (var nestedTypeHandle in typeDefinition.GetNestedTypes())
                {
                    var nestedType = metadataReader.GetTypeDefinition(nestedTypeHandle);
                    if (!IsStaticClass(nestedType) ||
                        !IsPublicType(nestedType) ||
                        !HasSpecialName(nestedType))
                    {
                        continue;
                    }

                    // Look for the special '<Extension>$' marker method which is how we can determine
                    // which type is being extended by the extension block.
                    foreach (var child in nestedType.GetMethods())
                    {
                        var method = metadataReader.GetMethodDefinition(child);
                        if ((method.Attributes & MethodAttributes.SpecialName) == 0 &&
                            (method.Attributes & MethodAttributes.RTSpecialName) == 0)
                        {
                            continue;
                        }

                        // has to be `public static "<Extension>$"(parameter)` (with exactly one parameter). 
                        if ((method.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public ||
                            (method.Attributes & MethodAttributes.Static) == 0 ||
                            method.GetParameters().Count != 1 ||
                            method.GetCustomAttributes().Count == 0)
                        {
                            continue;
                        }

                        var methodName = metadataReader.GetString(method.Name);
                        if (methodName != "<Extension>$")
                            continue;

                        // Decode method signature to get the receiver type name (i.e. type name for the first parameter)
                        var blob = metadataReader.GetBlobReader(method.Signature);
                        var decoder = new SignatureDecoder<ParameterTypeInfo, object?>(ParameterTypeInfoProvider.Instance, metadataReader, genericContext: null);
                        var signature = decoder.DecodeMethodSignature(ref blob);

                        if (signature.ParameterTypes is not [var parameterType])
                            continue;

                        return parameterType;
                    }
                }

                return null;
            }
        }

        private readonly void LookupMetadataDefinitions(
            MetadataReader metadataReader,
            NamespaceDefinition namespaceDefinition,
            OrderPreservingMultiDictionary<string, MetadataDefinition> definitionMap)
        {
            foreach (var child in namespaceDefinition.NamespaceDefinitions)
            {
                var definition = MetadataDefinition.Create(metadataReader, child);
                definitionMap.Add(definition.Name, definition);
            }

            foreach (var child in namespaceDefinition.TypeDefinitions)
            {
                var typeDefinition = metadataReader.GetTypeDefinition(child);
                if (IsPublicType(typeDefinition))
                {
                    var definition = MetadataDefinition.Create(metadataReader, typeDefinition);
                    definitionMap.Add(definition.Name, definition);
                    _allTypeDefinitions.Add(definition);
                }
            }
        }

        private static bool IsPublicType(TypeDefinition typeDefinition)
        {
            var masked = typeDefinition.Attributes & TypeAttributes.VisibilityMask;
            return masked is TypeAttributes.Public or TypeAttributes.NestedPublic;
        }

        private void PopulateInheritanceMap(MetadataReader metadataReader)
        {
            foreach (var typeDefinition in _allTypeDefinitions)
            {
                Debug.Assert(typeDefinition.Kind == MetadataDefinitionKind.Type);
                PopulateInheritance(metadataReader, typeDefinition);
            }
        }

        private void PopulateInheritance(
            MetadataReader metadataReader,
            MetadataDefinition metadataTypeDefinition)
        {
            var derivedTypeDefinition = metadataTypeDefinition.Type;
            var interfaceImplHandles = derivedTypeDefinition.GetInterfaceImplementations();

            if (derivedTypeDefinition.BaseType.IsNil &&
                interfaceImplHandles.Count == 0)
            {
                return;
            }

            var derivedTypeSimpleName = metadataTypeDefinition.Name;

            PopulateInheritance(metadataReader, derivedTypeSimpleName, derivedTypeDefinition.BaseType);

            foreach (var interfaceImplHandle in interfaceImplHandles)
            {
                if (!interfaceImplHandle.IsNil)
                {
                    var interfaceImpl = metadataReader.GetInterfaceImplementation(interfaceImplHandle);
                    PopulateInheritance(metadataReader, derivedTypeSimpleName, interfaceImpl.Interface);
                }
            }
        }

        private readonly void PopulateInheritance(
            MetadataReader metadataReader,
            string derivedTypeSimpleName,
            EntityHandle baseTypeOrInterfaceHandle)
        {
            if (baseTypeOrInterfaceHandle.IsNil)
            {
                return;
            }

            var baseTypeNameParts = s_stringListPool.Allocate();
            try
            {
                AddBaseTypeNameParts(metadataReader, baseTypeOrInterfaceHandle, baseTypeNameParts);
                if (baseTypeNameParts.Count > 0 &&
                    baseTypeNameParts.TrueForAll(s_isNotNullOrEmpty))
                {
                    var lastPart = baseTypeNameParts.Last();
                    if (!_inheritanceMap.Contains(lastPart, derivedTypeSimpleName))
                    {
                        _inheritanceMap.Add(baseTypeNameParts.Last(), derivedTypeSimpleName);
                    }

                    // The parent/child map may not know about this base-type yet (for example,
                    // if the base type is a reference to a type outside of this assembly).
                    // Add the base type to our map so we'll be able to resolve it later if 
                    // requested. 
                    EnsureParentsAndChildren(baseTypeNameParts);
                }
            }
            finally
            {
                s_stringListPool.ClearAndFree(baseTypeNameParts);
            }
        }

        private static void AddBaseTypeNameParts(
            MetadataReader metadataReader,
            EntityHandle baseTypeOrInterfaceHandle,
            List<string> simpleNames)
        {
            var typeDefOrRefHandle = GetTypeDefOrRefHandle(metadataReader, baseTypeOrInterfaceHandle);
            if (typeDefOrRefHandle.Kind == HandleKind.TypeDefinition)
            {
                AddTypeDefinitionNameParts(metadataReader, (TypeDefinitionHandle)typeDefOrRefHandle, simpleNames);
            }
            else if (typeDefOrRefHandle.Kind == HandleKind.TypeReference)
            {
                AddTypeReferenceNameParts(metadataReader, (TypeReferenceHandle)typeDefOrRefHandle, simpleNames);
            }
        }

        private static void AddTypeDefinitionNameParts(
            MetadataReader metadataReader,
            TypeDefinitionHandle handle,
            List<string> simpleNames)
        {
            var typeDefinition = metadataReader.GetTypeDefinition(handle);
            var declaringType = typeDefinition.GetDeclaringType();
            if (declaringType.IsNil)
            {
                // Not a nested type, just add the containing namespace.
                AddNamespaceParts(metadataReader, typeDefinition.NamespaceDefinition, simpleNames);
            }
            else
            {
                // We're a nested type, recurse and add the type we're declared in.
                // It will handle adding the namespace properly.
                AddTypeDefinitionNameParts(metadataReader, declaringType, simpleNames);
            }

            // Now add the simple name of the type itself.
            simpleNames.Add(GetMetadataNameWithoutBackticks(metadataReader, typeDefinition.Name));
        }

        private static void AddNamespaceParts(
            MetadataReader metadataReader,
            StringHandle namespaceHandle,
            List<string> simpleNames)
        {
            var blobReader = metadataReader.GetBlobReader(namespaceHandle);

            while (true)
            {
                var dotIndex = blobReader.IndexOf((byte)'.');
                unsafe
                {
                    // Note: we won't get any string sharing as we're just using the 
                    // default string decoded.  However, that's ok.  We only produce
                    // these strings when we first read metadata.  Then we create and
                    // persist our own index.  In the future when we read in that index
                    // there's no way for us to share strings between us and the 
                    // compiler at that point.
                    if (dotIndex == -1)
                    {
                        simpleNames.Add(MetadataStringDecoder.DefaultUTF8.GetString(
                            blobReader.CurrentPointer, blobReader.RemainingBytes));
                        return;
                    }
                    else
                    {
                        simpleNames.Add(MetadataStringDecoder.DefaultUTF8.GetString(
                            blobReader.CurrentPointer, dotIndex));
                        blobReader.Offset += dotIndex + 1;
                    }
                }
            }
        }

        private static void AddNamespaceParts(
            MetadataReader metadataReader,
            NamespaceDefinitionHandle namespaceHandle,
            List<string> simpleNames)
        {
            if (namespaceHandle.IsNil)
            {
                return;
            }

            var namespaceDefinition = metadataReader.GetNamespaceDefinition(namespaceHandle);
            AddNamespaceParts(metadataReader, namespaceDefinition.Parent, simpleNames);
            simpleNames.Add(metadataReader.GetString(namespaceDefinition.Name));
        }

        private static void AddTypeReferenceNameParts(
            MetadataReader metadataReader,
            TypeReferenceHandle handle,
            List<string> simpleNames)
        {
            var typeReference = metadataReader.GetTypeReference(handle);
            AddNamespaceParts(metadataReader, typeReference.Namespace, simpleNames);
            simpleNames.Add(GetMetadataNameWithoutBackticks(metadataReader, typeReference.Name));
        }

        private static EntityHandle GetTypeDefOrRefHandle(
            MetadataReader metadataReader,
            EntityHandle baseTypeOrInterfaceHandle)
        {
            switch (baseTypeOrInterfaceHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                case HandleKind.TypeReference:
                    return baseTypeOrInterfaceHandle;
                case HandleKind.TypeSpecification:
                    return FirstEntityHandleProvider.Instance.GetTypeFromSpecification(
                        metadataReader, (TypeSpecificationHandle)baseTypeOrInterfaceHandle);
                default:
                    return default;
            }
        }

        private readonly void EnsureParentsAndChildren(List<string> simpleNames)
        {
            var currentNode = _rootNode;

            foreach (var simpleName in simpleNames)
            {
                var childNode = GetOrCreateChildNode(currentNode, simpleName);
                currentNode = childNode;
            }
        }

        private readonly MetadataNode GetOrCreateChildNode(
           MetadataNode currentNode, string simpleName)
        {
            if (_parentToChildren.TryGetValue(currentNode, static (childNode, simpleName) => childNode.Name == simpleName, simpleName, out var childNode))
            {
                // Found an existing child node.  Just return that and all 
                // future parts off of it.
                return childNode;
            }

            // Couldn't find a child node with this name.  Make a new node for
            // it and return that for all future parts to be added to.
            var newChildNode = MetadataNode.Allocate(simpleName);
            _parentToChildren.Add(currentNode, newChildNode);
            return newChildNode;
        }

        private readonly ImmutableArray<BuilderNode> GenerateUnsortedNodes(MultiDictionary<string, ExtensionMemberInfo> receiverTypeNameToMemberMap)
        {
            var unsortedNodes = ArrayBuilder<BuilderNode>.GetInstance();
            unsortedNodes.Add(BuilderNode.RootNode);

            AddUnsortedNodes(unsortedNodes, receiverTypeNameToMemberMap, parentNode: _rootNode, parentIndex: 0, fullyQualifiedContainerName: _containsExtensionMembers ? "" : null);
            return unsortedNodes.ToImmutableAndFree();
        }

        private readonly void AddUnsortedNodes(ArrayBuilder<BuilderNode> unsortedNodes,
            MultiDictionary<string, ExtensionMemberInfo> receiverTypeNameToMemberMap,
            MetadataNode parentNode,
            int parentIndex,
            string? fullyQualifiedContainerName)
        {
            foreach (var child in _parentToChildren[parentNode])
            {
                var childNode = new BuilderNode(child.Name, parentIndex);
                var childIndex = unsortedNodes.Count;
                unsortedNodes.Add(childNode);

                if (fullyQualifiedContainerName != null)
                {
                    foreach (var (parameterTypeInfo, isModernExtension) in _extensionMemberToParameterTypeInfo[child])
                    {
                        // We do not differentiate array of different kinds for simplicity.
                        // e.g. int[], int[][], int[,], etc. are all represented as int[] in the index.
                        // similar for complex receiver types, "[]" means it's an array type, "" otherwise.
                        var parameterTypeName = (parameterTypeInfo.IsComplexType, parameterTypeInfo.IsArray) switch
                        {
                            (true, true) => Extensions.ComplexArrayReceiverTypeName,                          // complex array type, e.g. "T[,]"
                            (true, false) => Extensions.ComplexReceiverTypeName,                              // complex non-array type, e.g. "T"
                            (false, true) => parameterTypeInfo.Name + Extensions.ArrayReceiverTypeNameSuffix, // simple array type, e.g. "int[][,]"
                            (false, false) => parameterTypeInfo.Name                                          // simple non-array type, e.g. "int"
                        };

                        // Symbol index expects modern extensions to be of the form `NS.StaticClass.extension(...)`.
                        // So we mimic that here so modern members can be properly found.
                        var containerName = isModernExtension
                            ? fullyQualifiedContainerName + ".extension()"
                            : fullyQualifiedContainerName;

                        receiverTypeNameToMemberMap.Add(
                            parameterTypeName,
                            new ExtensionMemberInfo(containerName, child.Name));
                    }
                }

                AddUnsortedNodes(unsortedNodes, receiverTypeNameToMemberMap, child, childIndex, Concat(fullyQualifiedContainerName, child.Name));
            }

            [return: NotNullIfNotNull(nameof(containerName))]
            static string? Concat(string? containerName, string name)
            {
                if (containerName == null)
                {
                    return null;
                }

                if (containerName.Length == 0)
                {
                    return name;
                }

                return containerName + "." + name;
            }
        }
    }

    private sealed class MetadataNode
    {
        private static readonly ObjectPool<MetadataNode> s_pool = SharedPools.Default<MetadataNode>();

        /// <summary>
        /// Represent this as non-null because that will be true when this is not in a pool and it is being used by
        /// other services.
        /// </summary>
        public string Name { get; private set; } = null!;

        public static MetadataNode Allocate(string name)
        {
            var node = s_pool.Allocate();
            Debug.Assert(node.Name == null);
            node.Name = name;
            return node;
        }

        public static void Free(MetadataNode node)
        {
            Debug.Assert(node.Name != null);
            node.Name = null!;
            s_pool.Free(node);
        }
    }

    private enum MetadataDefinitionKind
    {
        Namespace,
        Type,
        Member,
    }

    private readonly struct MetadataDefinition(
        MetadataDefinitionKind kind,
        string name,
        bool isModernExtension = false,
        ParameterTypeInfo receiverTypeInfo = default,
        NamespaceDefinition @namespace = default,
        TypeDefinition type = default)
    {
        public string Name { get; } = name;
        public MetadataDefinitionKind Kind { get; } = kind;

        /// <summary>
        /// Only applies to member kind. Represents the type info of the first parameter.
        /// </summary>
        public ParameterTypeInfo ReceiverTypeInfo { get; } = receiverTypeInfo;
        public bool IsModernExtension { get; } = isModernExtension;

        public NamespaceDefinition Namespace { get; } = @namespace;
        public TypeDefinition Type { get; } = type;

        public static MetadataDefinition Create(
            MetadataReader reader, NamespaceDefinitionHandle namespaceHandle)
        {
            var definition = reader.GetNamespaceDefinition(namespaceHandle);
            return new MetadataDefinition(
                MetadataDefinitionKind.Namespace,
                reader.GetString(definition.Name),
                @namespace: definition);
        }

        public static MetadataDefinition Create(
            MetadataReader reader, TypeDefinition definition)
        {
            var typeName = GetMetadataNameWithoutBackticks(reader, definition.Name);
            return new MetadataDefinition(MetadataDefinitionKind.Type, typeName, type: definition);
        }
    }
}
