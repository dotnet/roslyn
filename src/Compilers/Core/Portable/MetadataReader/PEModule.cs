// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A set of helpers for extracting elements from metadata.
    /// This type is not responsible for managing the underlying storage
    /// backing the PE image.
    /// </summary>
    internal sealed class PEModule : IDisposable
    {
        /// <summary>
        /// We need to store reference to the module metadata to keep the metadata alive while 
        /// symbols have reference to PEModule.
        /// </summary>
        private readonly ModuleMetadata _owner;

        // Either we have PEReader or we have pointer and size of the metadata blob:
        private readonly PEReader _peReaderOpt;
        private readonly IntPtr _metadataPointerOpt;
        private readonly int _metadataSizeOpt;

        private MetadataReader _lazyMetadataReader;

        private ImmutableArray<AssemblyIdentity> _lazyAssemblyReferences;

        private static readonly Dictionary<string, (int FirstIndex, int SecondIndex)> s_sharedEmptyForwardedTypes = new Dictionary<string, (int FirstIndex, int SecondIndex)>();
        private static readonly Dictionary<string, (string OriginalName, int FirstIndex, int SecondIndex)> s_sharedEmptyCaseInsensitiveForwardedTypes = new Dictionary<string, (string OriginalName, int FirstIndex, int SecondIndex)>();

        /// <summary>
        /// This is a tuple for optimization purposes. In valid cases, we need to store
        /// only one assembly index per type. However, if we found more than one, we
        /// keep a second one as well to use it for error reporting.
        /// We use -1 in case there was no forward.
        /// </summary>
        private Dictionary<string, (int FirstIndex, int SecondIndex)> _lazyForwardedTypesToAssemblyIndexMap;

        /// <summary>
        /// Case-insensitive version of <see cref="_lazyForwardedTypesToAssemblyIndexMap"/>, only populated if case-insensitive search is
        /// requested. We only keep the first instance of a type name, regardless of case, as this is only used for error recovery purposes
        /// in VB.
        /// </summary>
        private Dictionary<string, (string OriginalName, int FirstIndex, int SecondIndex)> _lazyCaseInsensitiveForwardedTypesToAssemblyIndexMap;

        private readonly Lazy<IdentifierCollection> _lazyTypeNameCollection;
        private readonly Lazy<IdentifierCollection> _lazyNamespaceNameCollection;

        private string _lazyName;
        private bool _isDisposed;

        /// <summary>
        /// Using <see cref="ThreeState"/> as a type for atomicity.
        /// </summary>
        private ThreeState _lazyContainsNoPiaLocalTypes;

        /// <summary>
        /// If bitmap is not null, each bit indicates whether a TypeDef 
        /// with corresponding RowId has been checked if it is a NoPia 
        /// local type. If the bit is 1, local type will have an entry 
        /// in m_lazyTypeDefToTypeIdentifierMap.
        /// </summary>
        private int[] _lazyNoPiaLocalTypeCheckBitMap;

        /// <summary>
        /// For each TypeDef that has 1 in m_lazyNoPiaLocalTypeCheckBitMap,
        /// this map stores corresponding TypeIdentifier AttributeInfo. 
        /// </summary>
        private ConcurrentDictionary<TypeDefinitionHandle, AttributeInfo> _lazyTypeDefToTypeIdentifierMap;

        // The module can be used by different compilations or different versions of the "same"
        // compilation, which use different hash algorithms. Let's cache result for each distinct 
        // algorithm.
        private readonly CryptographicHashProvider _hashesOpt;

#nullable enable
        private delegate bool AttributeValueExtractor<T>(out T value, ref BlobReader sigReader);
        private static readonly AttributeValueExtractor<string?> s_attributeStringValueExtractor = CrackStringInAttributeValue;
        private static readonly AttributeValueExtractor<(int, int)> s_attributeIntAndIntValueExtractor = CrackIntAndIntInAttributeValue;
        private static readonly AttributeValueExtractor<StringAndInt> s_attributeStringAndIntValueExtractor = CrackStringAndIntInAttributeValue;
        private static readonly AttributeValueExtractor<(string?, string?)> s_attributeStringAndStringValueExtractor = CrackStringAndStringInAttributeValue;
        private static readonly AttributeValueExtractor<bool> s_attributeBooleanValueExtractor = CrackBooleanInAttributeValue;
        private static readonly AttributeValueExtractor<byte> s_attributeByteValueExtractor = CrackByteInAttributeValue;
        private static readonly AttributeValueExtractor<short> s_attributeShortValueExtractor = CrackShortInAttributeValue;
        private static readonly AttributeValueExtractor<int> s_attributeIntValueExtractor = CrackIntInAttributeValue;
        private static readonly AttributeValueExtractor<long> s_attributeLongValueExtractor = CrackLongInAttributeValue;
        // Note: not a general purpose helper
        private static readonly AttributeValueExtractor<decimal> s_decimalValueInDecimalConstantAttributeExtractor = CrackDecimalInDecimalConstantAttribute;
        private static readonly AttributeValueExtractor<ImmutableArray<bool>> s_attributeBoolArrayValueExtractor = CrackBoolArrayInAttributeValue;
        private static readonly AttributeValueExtractor<ImmutableArray<byte>> s_attributeByteArrayValueExtractor = CrackByteArrayInAttributeValue;
        private static readonly AttributeValueExtractor<ImmutableArray<string?>> s_attributeStringArrayValueExtractor = CrackStringArrayInAttributeValue;
        private static readonly AttributeValueExtractor<ObsoleteAttributeData?> s_attributeDeprecatedDataExtractor = CrackDeprecatedAttributeData;
        private static readonly AttributeValueExtractor<BoolAndStringArrayData> s_attributeBoolAndStringArrayValueExtractor = CrackBoolAndStringArrayInAttributeValue;
        private static readonly AttributeValueExtractor<BoolAndStringData> s_attributeBoolAndStringValueExtractor = CrackBoolAndStringInAttributeValue;

        internal readonly struct BoolAndStringArrayData
        {
            public BoolAndStringArrayData(bool sense, ImmutableArray<string?> strings)
            {
                Sense = sense;
                Strings = strings;
            }

            public readonly bool Sense;
            public readonly ImmutableArray<string?> Strings;
        }

        internal readonly struct BoolAndStringData
        {
            public BoolAndStringData(bool sense, string? @string)
            {
                Sense = sense;
                String = @string;
            }

            public readonly bool Sense;
            public readonly string? String;
        }
#nullable disable

        // 'ignoreAssemblyRefs' is used by the EE only, when debugging
        // .NET Native, where the corlib may have assembly references
        // (see https://github.com/dotnet/roslyn/issues/13275).
        internal PEModule(ModuleMetadata owner, PEReader peReader, IntPtr metadataOpt, int metadataSizeOpt, bool includeEmbeddedInteropTypes, bool ignoreAssemblyRefs)
        {
            // shall not throw

            Debug.Assert((peReader == null) ^ (metadataOpt == IntPtr.Zero && metadataSizeOpt == 0));
            Debug.Assert(metadataOpt == IntPtr.Zero || metadataSizeOpt > 0);

            _owner = owner;
            _peReaderOpt = peReader;
            _metadataPointerOpt = metadataOpt;
            _metadataSizeOpt = metadataSizeOpt;
            _lazyTypeNameCollection = new Lazy<IdentifierCollection>(ComputeTypeNameCollection);
            _lazyNamespaceNameCollection = new Lazy<IdentifierCollection>(ComputeNamespaceNameCollection);
            _hashesOpt = (peReader != null) ? new PEHashProvider(peReader) : null;
            _lazyContainsNoPiaLocalTypes = includeEmbeddedInteropTypes ? ThreeState.False : ThreeState.Unknown;

            if (ignoreAssemblyRefs)
            {
                _lazyAssemblyReferences = ImmutableArray<AssemblyIdentity>.Empty;
            }
        }

        private sealed class PEHashProvider : CryptographicHashProvider
        {
            private readonly PEReader _peReader;

            public PEHashProvider(PEReader peReader)
            {
                Debug.Assert(peReader != null);
                _peReader = peReader;
            }

            internal override unsafe ImmutableArray<byte> ComputeHash(HashAlgorithm algorithm)
            {
                PEMemoryBlock block = _peReader.GetEntireImage();
                byte[] hash;

                using (var stream = new ReadOnlyUnmanagedMemoryStream(_peReader, (IntPtr)block.Pointer, block.Length))
                {
                    hash = algorithm.ComputeHash(stream);
                }

                return ImmutableArray.Create(hash);
            }
        }

        internal bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }

        public void Dispose()
        {
            _isDisposed = true;

            _peReaderOpt?.Dispose();
        }

        // for testing
        internal PEReader PEReaderOpt
        {
            get
            {
                return _peReaderOpt;
            }
        }

        internal MetadataReader MetadataReader
        {
            get
            {
                if (_lazyMetadataReader == null)
                {
                    InitializeMetadataReader();
                }

                if (_isDisposed)
                {
                    // Without locking, which might be expensive, we can't guarantee that the underlying memory 
                    // won't be accessed after the metadata object is disposed. However we can do a cheap check here that 
                    // handles most cases.
                    ThrowMetadataDisposed();
                }

                return _lazyMetadataReader;
            }
        }

        private unsafe void InitializeMetadataReader()
        {
            MetadataReader newReader;

            // PEModule is either created with metadata memory block or a PE reader.
            if (_metadataPointerOpt != IntPtr.Zero)
            {
                newReader = new MetadataReader((byte*)_metadataPointerOpt, _metadataSizeOpt, MetadataReaderOptions.ApplyWindowsRuntimeProjections, StringTableDecoder.Instance);
            }
            else
            {
                Debug.Assert(_peReaderOpt != null);

                // A workaround for https://github.com/dotnet/corefx/issues/1815    
                bool hasMetadata;
                try
                {
                    hasMetadata = _peReaderOpt.HasMetadata;
                }
                catch
                {
                    hasMetadata = false;
                }

                if (!hasMetadata)
                {
                    throw new BadImageFormatException(CodeAnalysisResources.PEImageDoesntContainManagedMetadata);
                }

                newReader = _peReaderOpt.GetMetadataReader(MetadataReaderOptions.ApplyWindowsRuntimeProjections, StringTableDecoder.Instance);
            }

            Interlocked.CompareExchange(ref _lazyMetadataReader, newReader, null);
        }

        private static void ThrowMetadataDisposed()
        {
            throw new ObjectDisposedException(nameof(ModuleMetadata));
        }

        #region Module level properties and methods

        internal bool IsManifestModule
        {
            get
            {
                return MetadataReader.IsAssembly;
            }
        }

        internal bool IsLinkedModule
        {
            get
            {
                return !MetadataReader.IsAssembly;
            }
        }

        internal bool IsCOFFOnly
        {
            get
            {
                // default value if we only have metadata
                if (_peReaderOpt == null)
                {
                    return false;
                }

                return _peReaderOpt.PEHeaders.IsCoffOnly;
            }
        }

        /// <summary>
        /// Target architecture of the machine.
        /// </summary>
        internal Machine Machine
        {
            get
            {
                // platform agnostic if we only have metadata
                if (_peReaderOpt == null)
                {
                    return Machine.I386;
                }

                return _peReaderOpt.PEHeaders.CoffHeader.Machine;
            }
        }

        /// <summary>
        /// Indicates that this PE file makes Win32 calls. See CorPEKind.pe32BitRequired for more information (http://msdn.microsoft.com/en-us/library/ms230275.aspx).
        /// </summary>
        internal bool Bit32Required
        {
            get
            {
                // platform agnostic if we only have metadata
                if (_peReaderOpt == null)
                {
                    return false;
                }

                return (_peReaderOpt.PEHeaders.CorHeader.Flags & CorFlags.Requires32Bit) != 0;
            }
        }

        internal ImmutableArray<byte> GetHash(AssemblyHashAlgorithm algorithmId)
        {
            Debug.Assert(_hashesOpt != null);
            return _hashesOpt.GetHash(algorithmId);
        }

        #endregion

        #region ModuleDef helpers

        internal string Name
        {
            get
            {
                if (_lazyName == null)
                {
                    _lazyName = MetadataReader.GetString(MetadataReader.GetModuleDefinition().Name);
                }

                return _lazyName;
            }
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal Guid GetModuleVersionIdOrThrow()
        {
            return MetadataReader.GetModuleVersionIdOrThrow();
        }

        #endregion

        #region ModuleRef, File helpers

        /// <summary>
        /// Returns the names of linked managed modules.
        /// </summary>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal ImmutableArray<string> GetMetadataModuleNamesOrThrow()
        {
            var builder = ArrayBuilder<string>.GetInstance();
            try
            {
                foreach (var fileHandle in MetadataReader.AssemblyFiles)
                {
                    var file = MetadataReader.GetAssemblyFile(fileHandle);
                    if (!file.ContainsMetadata)
                    {
                        continue;
                    }

                    string moduleName = MetadataReader.GetString(file.Name);
                    if (!MetadataHelpers.IsValidMetadataFileName(moduleName))
                    {
                        throw new BadImageFormatException(string.Format(CodeAnalysisResources.InvalidModuleName, this.Name, moduleName));
                    }

                    builder.Add(moduleName);
                }

                return builder.ToImmutable();
            }
            finally
            {
                builder.Free();
            }
        }

        /// <summary>
        /// Returns names of referenced modules.
        /// </summary>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal IEnumerable<string> GetReferencedManagedModulesOrThrow()
        {
            HashSet<EntityHandle> nameTokens = new HashSet<EntityHandle>();
            foreach (var handle in MetadataReader.TypeReferences)
            {
                TypeReference typeRef = MetadataReader.GetTypeReference(handle);
                EntityHandle scope = typeRef.ResolutionScope;
                if (scope.Kind == HandleKind.ModuleReference)
                {
                    nameTokens.Add(scope);
                }
            }

            foreach (var token in nameTokens)
            {
                yield return this.GetModuleRefNameOrThrow((ModuleReferenceHandle)token);
            }
        }

        internal ImmutableArray<EmbeddedResource> GetEmbeddedResourcesOrThrow()
        {
            if (MetadataReader.ManifestResources.Count == 0)
            {
                return ImmutableArray<EmbeddedResource>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<EmbeddedResource>();
            foreach (var handle in MetadataReader.ManifestResources)
            {
                var resource = MetadataReader.GetManifestResource(handle);
                if (resource.Implementation.IsNil)
                {
                    string resourceName = MetadataReader.GetString(resource.Name);
                    builder.Add(new EmbeddedResource((uint)resource.Offset, resource.Attributes, resourceName));
                }
            }

            return builder.ToImmutable();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public string GetModuleRefNameOrThrow(ModuleReferenceHandle moduleRef)
        {
            return MetadataReader.GetString(MetadataReader.GetModuleReference(moduleRef).Name);
        }

        #endregion

        #region AssemblyRef helpers

        // The array is sorted by AssemblyRef RowId, starting with RowId=1 and doesn't have any RowId gaps.
        public ImmutableArray<AssemblyIdentity> ReferencedAssemblies
        {
            get
            {
                if (_lazyAssemblyReferences == null)
                {
                    _lazyAssemblyReferences = this.MetadataReader.GetReferencedAssembliesOrThrow();
                }

                return _lazyAssemblyReferences;
            }
        }

        #endregion

        #region PE Header helpers

        internal string MetadataVersion
        {
            get { return MetadataReader.MetadataVersion; }
        }

        #endregion

        #region Heaps

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal BlobReader GetMemoryReaderOrThrow(BlobHandle blob)
        {
            return MetadataReader.GetBlobReader(blob);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal string GetFullNameOrThrow(StringHandle namespaceHandle, StringHandle nameHandle)
        {
            var attributeTypeName = MetadataReader.GetString(nameHandle);
            var attributeTypeNamespaceName = MetadataReader.GetString(namespaceHandle);

            return MetadataHelpers.BuildQualifiedName(attributeTypeNamespaceName, attributeTypeName);
        }

        #endregion

        #region AssemblyDef helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal AssemblyIdentity ReadAssemblyIdentityOrThrow()
        {
            return MetadataReader.ReadAssemblyIdentityOrThrow();
        }

        #endregion

        #region TypeDef helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public TypeDefinitionHandle GetContainingTypeOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).GetDeclaringType();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public string GetTypeDefNameOrThrow(TypeDefinitionHandle typeDef)
        {
            TypeDefinition typeDefinition = MetadataReader.GetTypeDefinition(typeDef);
            string name = MetadataReader.GetString(typeDefinition.Name);
            Debug.Assert(name.Length == 0 || MetadataHelpers.IsValidMetadataIdentifier(name)); // Obfuscated assemblies can have types with empty names.

            // The problem is that the mangled name for a static machine type looks like 
            // "<" + methodName + ">d__" + uniqueId.However, methodName will have dots in 
            // it for explicit interface implementations (e.g. "<I.F>d__0").  Unfortunately, 
            // the native compiler emits such names in a very strange way: everything before 
            // the last dot goes in the namespace (!!) field of the typedef.Since state
            // machine types are always nested types and since nested types never have 
            // explicit namespaces (since they are in the same namespaces as their containing
            // types), it should be safe to check for a non-empty namespace name on a nested
            // type and prepend the namespace name and a dot to the type name.  After that, 
            // debugging support falls out.
            if (IsNestedTypeDefOrThrow(typeDef))
            {
                string namespaceName = MetadataReader.GetString(typeDefinition.Namespace);
                if (namespaceName.Length > 0)
                {
                    // As explained above, this is not really the qualified name - the namespace
                    // name is actually the part of the name that preceded the last dot (in bad
                    // metadata).
                    name = namespaceName + "." + name;
                }
            }

            return name;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public string GetTypeDefNamespaceOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetString(MetadataReader.GetTypeDefinition(typeDef).Namespace);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public EntityHandle GetTypeDefExtendsOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).BaseType;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public TypeAttributes GetTypeDefFlagsOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).Attributes;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public GenericParameterHandleCollection GetTypeDefGenericParamsOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).GetGenericParameters();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public bool HasGenericParametersOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).GetGenericParameters().Count > 0;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public void GetTypeDefPropsOrThrow(
            TypeDefinitionHandle typeDef,
            out string name,
            out string @namespace,
            out TypeAttributes flags,
            out EntityHandle extends)
        {
            TypeDefinition row = MetadataReader.GetTypeDefinition(typeDef);
            name = MetadataReader.GetString(row.Name);
            @namespace = MetadataReader.GetString(row.Namespace);
            flags = row.Attributes;
            extends = row.BaseType;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal bool IsNestedTypeDefOrThrow(TypeDefinitionHandle typeDef)
        {
            return IsNestedTypeDefOrThrow(MetadataReader, typeDef);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private static bool IsNestedTypeDefOrThrow(MetadataReader metadataReader, TypeDefinitionHandle typeDef)
        {
            return IsNested(metadataReader.GetTypeDefinition(typeDef).Attributes);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal bool IsInterfaceOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).Attributes.IsInterface();
        }

        private readonly struct TypeDefToNamespace
        {
            internal readonly TypeDefinitionHandle TypeDef;
            internal readonly NamespaceDefinitionHandle NamespaceHandle;

            internal TypeDefToNamespace(TypeDefinitionHandle typeDef, NamespaceDefinitionHandle namespaceHandle)
            {
                TypeDef = typeDef;
                NamespaceHandle = namespaceHandle;
            }
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private IEnumerable<TypeDefToNamespace> GetTypeDefsOrThrow(bool topLevelOnly)
        {
            foreach (var typeDef in MetadataReader.TypeDefinitions)
            {
                var row = MetadataReader.GetTypeDefinition(typeDef);

                if (topLevelOnly && IsNested(row.Attributes))
                {
                    continue;
                }

                yield return new TypeDefToNamespace(typeDef, row.NamespaceDefinition);
            }
        }

        /// <summary>
        /// The function groups types defined in the module by their fully-qualified namespace name.
        /// The case-sensitivity of the grouping depends upon the provided StringComparer.
        /// 
        /// The sequence is sorted by name by using provided comparer. Therefore, if there are multiple 
        /// groups for a namespace name (e.g. because they differ in case), the groups are going to be 
        /// adjacent to each other. 
        /// 
        /// Empty string is used as namespace name for types in the Global namespace. Therefore, all types 
        /// in the Global namespace, if any, should be in the first group (assuming a reasonable StringComparer).
        /// </summary>
        /// Comparer to sort the groups.
        /// <param name="nameComparer">
        /// </param>
        /// <returns>A sorted list of TypeDef row ids, grouped by fully-qualified namespace name.</returns>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal IEnumerable<IGrouping<string, TypeDefinitionHandle>> GroupTypesByNamespaceOrThrow(StringComparer nameComparer)
        {
            // TODO: Consider if we should cache the result (not the IEnumerable, but the actual values).

            // NOTE:  Rather than use a sorted dictionary, we accumulate the groupings in a normal dictionary
            // and then sort the list.  We do this so that namespaces with distinct names are not
            // merged, even if they are equal according to the provided comparer.  This improves the error
            // experience because types retain their exact namespaces.

            Dictionary<string, ArrayBuilder<TypeDefinitionHandle>> namespaces = new Dictionary<string, ArrayBuilder<TypeDefinitionHandle>>();

            GetTypeNamespaceNamesOrThrow(namespaces);
            GetForwardedTypeNamespaceNamesOrThrow(namespaces);

            var result = new ArrayBuilder<IGrouping<string, TypeDefinitionHandle>>(namespaces.Count);

            foreach (var pair in namespaces)
            {
                result.Add(new Grouping<string, TypeDefinitionHandle>(pair.Key, pair.Value ?? SpecializedCollections.EmptyEnumerable<TypeDefinitionHandle>()));
            }

            result.Sort(new TypesByNamespaceSortComparer(nameComparer));
            return result;
        }

        internal class TypesByNamespaceSortComparer : IComparer<IGrouping<string, TypeDefinitionHandle>>
        {
            private readonly StringComparer _nameComparer;

            public TypesByNamespaceSortComparer(StringComparer nameComparer)
            {
                _nameComparer = nameComparer;
            }

            public int Compare(IGrouping<string, TypeDefinitionHandle> left, IGrouping<string, TypeDefinitionHandle> right)
            {
                if (left == right)
                {
                    return 0;
                }

                int result = _nameComparer.Compare(left.Key, right.Key);

                if (result == 0)
                {
                    var fLeft = left.FirstOrDefault();
                    var fRight = right.FirstOrDefault();

                    if (fLeft.IsNil ^ fRight.IsNil)
                    {
                        result = fLeft.IsNil ? +1 : -1;
                    }
                    else
                    {
                        result = HandleComparer.Default.Compare(fLeft, fRight);
                    }

                    if (result == 0)
                    {
                        // This can only happen when both are for forwarded types.
                        Debug.Assert(left.IsEmpty() && right.IsEmpty());
                        result = string.CompareOrdinal(left.Key, right.Key);
                    }
                }

                Debug.Assert(result != 0);
                return result;
            }
        }

        /// <summary>
        /// Groups together the RowIds of types in a given namespaces.  The types considered are
        /// those defined in this module.
        /// </summary>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private void GetTypeNamespaceNamesOrThrow(Dictionary<string, ArrayBuilder<TypeDefinitionHandle>> namespaces)
        {
            // PERF: Group by namespace handle so we only have to allocate one string for every namespace
            var namespaceHandles = new Dictionary<NamespaceDefinitionHandle, ArrayBuilder<TypeDefinitionHandle>>(NamespaceHandleEqualityComparer.Singleton);
            foreach (TypeDefToNamespace pair in GetTypeDefsOrThrow(topLevelOnly: true))
            {
                NamespaceDefinitionHandle nsHandle = pair.NamespaceHandle;
                TypeDefinitionHandle typeDef = pair.TypeDef;

                ArrayBuilder<TypeDefinitionHandle> builder;

                if (namespaceHandles.TryGetValue(nsHandle, out builder))
                {
                    builder.Add(typeDef);
                }
                else
                {
                    namespaceHandles.Add(nsHandle, new ArrayBuilder<TypeDefinitionHandle> { typeDef });
                }
            }

            foreach (var kvp in namespaceHandles)
            {
                string @namespace = MetadataReader.GetString(kvp.Key);

                ArrayBuilder<TypeDefinitionHandle> builder;

                if (namespaces.TryGetValue(@namespace, out builder))
                {
                    builder.AddRange(kvp.Value);
                }
                else
                {
                    namespaces.Add(@namespace, kvp.Value);
                }
            }
        }

        private class NamespaceHandleEqualityComparer : IEqualityComparer<NamespaceDefinitionHandle>
        {
            public static readonly NamespaceHandleEqualityComparer Singleton = new NamespaceHandleEqualityComparer();

            private NamespaceHandleEqualityComparer()
            {
            }

            public bool Equals(NamespaceDefinitionHandle x, NamespaceDefinitionHandle y)
            {
                return x == y;
            }

            public int GetHashCode(NamespaceDefinitionHandle obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Supplements the namespace-to-RowIDs map with the namespaces of forwarded types.
        /// These types will not have associated row IDs (represented as null, for efficiency).
        /// These namespaces are important because we want lookups of missing forwarded types
        /// to succeed far enough that we can actually find the type forwarder and provide
        /// information about the target assembly.
        /// 
        /// For example, consider the following forwarded type:
        /// 
        /// .class extern forwarder Namespace.Type {}
        /// 
        /// If this type is referenced in source as "Namespace.Type", then dev10 reports
        /// 
        /// error CS1070: The type name 'Namespace.Name' could not be found. This type has been 
        /// forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. 
        /// Consider adding a reference to that assembly.
        /// 
        /// If we did not include "Namespace" as a child of the global namespace of this module
        /// (the forwarding module), then Roslyn would report that the type "Namespace" was not
        /// found and say nothing about "Name" (because of the diagnostic already attached to 
        /// the qualifier).
        /// </summary>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private void GetForwardedTypeNamespaceNamesOrThrow(Dictionary<string, ArrayBuilder<TypeDefinitionHandle>> namespaces)
        {
            EnsureForwardTypeToAssemblyMap();

            foreach (var typeName in _lazyForwardedTypesToAssemblyIndexMap.Keys)
            {
                int index = typeName.LastIndexOf('.');
                string namespaceName = index >= 0 ? typeName.Substring(0, index) : "";
                if (!namespaces.ContainsKey(namespaceName))
                {
                    namespaces.Add(namespaceName, null);
                }
            }
        }

        private IdentifierCollection ComputeTypeNameCollection()
        {
            try
            {
                var allTypeDefs = GetTypeDefsOrThrow(topLevelOnly: false);
                var typeNames =
                    from typeDef in allTypeDefs
                    let metadataName = GetTypeDefNameOrThrow(typeDef.TypeDef)
                    let backtickIndex = metadataName.IndexOf('`')
                    select backtickIndex < 0 ? metadataName : metadataName.Substring(0, backtickIndex);

                return new IdentifierCollection(typeNames);
            }
            catch (BadImageFormatException)
            {
                return new IdentifierCollection();
            }
        }

        private IdentifierCollection ComputeNamespaceNameCollection()
        {
            try
            {
                var allTypeIds = GetTypeDefsOrThrow(topLevelOnly: true);
                var fullNamespaceNames =
                    from id in allTypeIds
                    where !id.NamespaceHandle.IsNil
                    select MetadataReader.GetString(id.NamespaceHandle);

                var namespaceNames =
                    from fullName in fullNamespaceNames.Distinct()
                    from name in fullName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                    select name;

                return new IdentifierCollection(namespaceNames);
            }
            catch (BadImageFormatException)
            {
                return new IdentifierCollection();
            }
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal ImmutableArray<TypeDefinitionHandle> GetNestedTypeDefsOrThrow(TypeDefinitionHandle container)
        {
            return MetadataReader.GetTypeDefinition(container).GetNestedTypes();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal MethodImplementationHandleCollection GetMethodImplementationsOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).GetMethodImplementations();
        }

        /// <summary>
        /// Returns a collection of interfaces implemented by given type.
        /// </summary>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal InterfaceImplementationHandleCollection GetInterfaceImplementationsOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).GetInterfaceImplementations();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal MethodDefinitionHandleCollection GetMethodsOfTypeOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).GetMethods();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal PropertyDefinitionHandleCollection GetPropertiesOfTypeOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).GetProperties();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal EventDefinitionHandleCollection GetEventsOfTypeOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).GetEvents();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal FieldDefinitionHandleCollection GetFieldsOfTypeOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).GetFields();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal EntityHandle GetBaseTypeOfTypeOrThrow(TypeDefinitionHandle typeDef)
        {
            return MetadataReader.GetTypeDefinition(typeDef).BaseType;
        }

        internal TypeLayout GetTypeLayout(TypeDefinitionHandle typeDef)
        {
            try
            {
                // CLI Spec 22.8.3:
                // The Class or ValueType indexed by Parent shall be SequentialLayout or ExplicitLayout. 
                // That is, AutoLayout types shall not own any rows in the ClassLayout table.
                var def = MetadataReader.GetTypeDefinition(typeDef);

                LayoutKind kind;
                switch (def.Attributes & TypeAttributes.LayoutMask)
                {
                    case TypeAttributes.SequentialLayout:
                        kind = LayoutKind.Sequential;
                        break;

                    case TypeAttributes.ExplicitLayout:
                        kind = LayoutKind.Explicit;
                        break;

                    case TypeAttributes.AutoLayout:
                        return default(TypeLayout);

                    default:
                        // TODO (tomat) report error:
                        return default(TypeLayout);
                }

                var layout = def.GetLayout();
                int size = layout.Size;
                int packingSize = layout.PackingSize;

                if (packingSize > byte.MaxValue)
                {
                    // TODO (tomat) report error:
                    packingSize = 0;
                }

                if (size < 0)
                {
                    // TODO (tomat) report error:
                    size = 0;
                }

                return new TypeLayout(kind, size, (byte)packingSize);
            }
            catch (BadImageFormatException)
            {
                return default(TypeLayout);
            }
        }

        internal bool IsNoPiaLocalType(TypeDefinitionHandle typeDef)
        {
            AttributeInfo attributeInfo;
            return IsNoPiaLocalType(typeDef, out attributeInfo);
        }

        internal bool HasParamArrayAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.ParamArrayAttribute).HasValue;
        }

        internal bool HasParamCollectionAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.ParamCollectionAttribute).HasValue;
        }

        internal bool HasIsReadOnlyAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.IsReadOnlyAttribute).HasValue;
        }

        internal bool HasDoesNotReturnAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.DoesNotReturnAttribute).HasValue;
        }

        internal bool HasIsUnmanagedAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.IsUnmanagedAttribute).HasValue;
        }

        internal bool HasExtensionAttribute(EntityHandle token, bool ignoreCase)
        {
            return FindTargetAttribute(token, ignoreCase ? AttributeDescription.CaseInsensitiveExtensionAttribute : AttributeDescription.CaseSensitiveExtensionAttribute).HasValue;
        }

        internal bool HasVisualBasicEmbeddedAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.VisualBasicEmbeddedAttribute).HasValue;
        }

        internal bool HasCodeAnalysisEmbeddedAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.CodeAnalysisEmbeddedAttribute).HasValue;
        }

        internal bool HasInterpolatedStringHandlerAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.InterpolatedStringHandlerAttribute).HasValue;
        }

        internal bool HasDefaultMemberAttribute(EntityHandle token, out string memberName)
        {
            return HasStringValuedAttribute(token, AttributeDescription.DefaultMemberAttribute, out memberName);
        }

        internal bool HasGuidAttribute(EntityHandle token, out string guidValue)
        {
            return HasStringValuedAttribute(token, AttributeDescription.GuidAttribute, out guidValue);
        }

        internal bool HasImportedFromTypeLibAttribute(EntityHandle token, out string libValue)
        {
            return HasStringValuedAttribute(token, AttributeDescription.ImportedFromTypeLibAttribute, out libValue);
        }

        internal bool HasPrimaryInteropAssemblyAttribute(EntityHandle token, out int majorValue, out int minorValue)
        {
            return HasIntAndIntValuedAttribute(token, AttributeDescription.PrimaryInteropAssemblyAttribute, out majorValue, out minorValue);
        }

        internal bool HasFixedBufferAttribute(EntityHandle token, out string elementTypeName, out int bufferSize)
        {
            return HasStringAndIntValuedAttribute(token, AttributeDescription.FixedBufferAttribute, out elementTypeName, out bufferSize);
        }

        internal bool HasAccessedThroughPropertyAttribute(EntityHandle token, out string propertyName)
        {
            return HasStringValuedAttribute(token, AttributeDescription.AccessedThroughPropertyAttribute, out propertyName);
        }

        internal bool HasRequiredAttributeAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.RequiredAttributeAttribute).HasValue;
        }

        internal bool HasCollectionBuilderAttribute(EntityHandle token, out string builderTypeName, out string methodName)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.CollectionBuilderAttribute);
            if (info.HasValue)
            {
                return TryExtractStringAndStringValueFromAttribute(info.Handle, out builderTypeName, out methodName);
            }

            builderTypeName = null;
            methodName = null;
            return false;
        }

        internal bool HasAttribute(EntityHandle token, AttributeDescription description)
        {
            return FindTargetAttribute(token, description).HasValue;
        }

        internal CustomAttributeHandle GetAttributeHandle(EntityHandle token, AttributeDescription description)
        {
            return FindTargetAttribute(token, description).Handle;
        }

        private static readonly ImmutableArray<bool> s_simpleTransformFlags = ImmutableArray.Create(true);

        internal bool HasDynamicAttribute(EntityHandle token, out ImmutableArray<bool> transformFlags)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.DynamicAttribute);
            Debug.Assert(!info.HasValue || info.SignatureIndex == 0 || info.SignatureIndex == 1);

            if (!info.HasValue)
            {
                transformFlags = default;
                return false;
            }

            if (info.SignatureIndex == 0)
            {
                transformFlags = s_simpleTransformFlags;
                return true;
            }

            return TryExtractBoolArrayValueFromAttribute(info.Handle, out transformFlags);
        }

        internal bool HasNativeIntegerAttribute(EntityHandle token, out ImmutableArray<bool> transformFlags)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.NativeIntegerAttribute);
            Debug.Assert(!info.HasValue || info.SignatureIndex == 0 || info.SignatureIndex == 1);

            if (!info.HasValue)
            {
                transformFlags = default;
                return false;
            }

            if (info.SignatureIndex == 0)
            {
                transformFlags = s_simpleTransformFlags;
                return true;
            }

            return TryExtractBoolArrayValueFromAttribute(info.Handle, out transformFlags);
        }

        internal bool HasScopedRefAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.ScopedRefAttribute).HasValue;
        }

        internal bool HasUnscopedRefAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.UnscopedRefAttribute).HasValue;
        }

        internal bool HasRefSafetyRulesAttribute(EntityHandle token, out int version, out bool foundAttributeType)
        {
            AttributeInfo info = FindTargetAttribute(MetadataReader, token, AttributeDescription.RefSafetyRulesAttribute, out foundAttributeType);
            if (info.HasValue)
            {
                Debug.Assert(info.SignatureIndex == 0);
                if (TryExtractValueFromAttribute(info.Handle, out int value, s_attributeIntValueExtractor))
                {
                    version = value;
                    return true;
                }
            }
            version = 0;
            return false;
        }

        internal bool HasInlineArrayAttribute(TypeDefinitionHandle token, out int length)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.InlineArrayAttribute);
            if (info.HasValue)
            {
                Debug.Assert(info.SignatureIndex == 0);
                if (TryExtractValueFromAttribute(info.Handle, out int value, s_attributeIntValueExtractor))
                {
                    length = value;
                    return true;
                }
            }

            length = 0;
            return false;
        }

        internal bool HasTupleElementNamesAttribute(EntityHandle token, out ImmutableArray<string> tupleElementNames)
        {
            var info = FindTargetAttribute(token, AttributeDescription.TupleElementNamesAttribute);
            Debug.Assert(!info.HasValue || info.SignatureIndex == 0 || info.SignatureIndex == 1);

            if (!info.HasValue)
            {
                tupleElementNames = default(ImmutableArray<string>);
                return false;
            }

            return TryExtractStringArrayValueFromAttribute(info.Handle, out tupleElementNames);
        }

        internal bool HasIsByRefLikeAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.IsByRefLikeAttribute).HasValue;
        }

        internal bool HasRequiresLocationAttribute(EntityHandle token)
        {
            return FindTargetAttribute(token, AttributeDescription.RequiresLocationAttribute).HasValue;
        }

        internal const string ByRefLikeMarker = "Types with embedded references are not supported in this version of your compiler.";
        internal const string RequiredMembersMarker = "Constructors of types with required members are not supported in this version of your compiler.";

        /// <remarks>Should be kept in sync with <see cref="IsMoreImportantObsoleteKind(ObsoleteAttributeKind, ObsoleteAttributeKind)"/></remarks>
        internal ObsoleteAttributeData TryGetDeprecatedOrExperimentalOrObsoleteAttribute(
            EntityHandle token,
            IAttributeNamedArgumentDecoder decoder,
            bool ignoreByRefLikeMarker,
            bool ignoreRequiredMemberMarker)
        {
            AttributeInfo info;

            info = FindTargetAttribute(token, AttributeDescription.DeprecatedAttribute);
            if (info.HasValue)
            {
                return TryExtractDeprecatedDataFromAttribute(info);
            }

            info = FindTargetAttribute(token, AttributeDescription.ObsoleteAttribute);
            if (info.HasValue)
            {
                ObsoleteAttributeData obsoleteData = TryExtractObsoleteDataFromAttribute(info, decoder);
                switch (obsoleteData?.Message)
                {
                    case ByRefLikeMarker when ignoreByRefLikeMarker:
                        return null;
                    case RequiredMembersMarker when ignoreRequiredMemberMarker:
                        return null;
                }
                return obsoleteData;
            }

            // [Windows.Foundation.Metadata.Experimental] is always a warning, not an error.
            info = FindTargetAttribute(token, AttributeDescription.WindowsExperimentalAttribute);
            if (info.HasValue)
            {
                return TryExtractWindowsExperimentalDataFromAttribute(info);
            }

            // [Experimental] is always a warning, not an error, so search for it last.
            info = FindTargetAttribute(token, AttributeDescription.ExperimentalAttribute);
            if (info.HasValue)
            {
                return TryExtractExperimentalDataFromAttribute(info, decoder);
            }

            return null;
        }

#nullable enable
        /// <summary>
        /// Indicates whether the first attribute should be prioritized over the second one.
        /// Same order of priority as
        ///   <see cref="TryGetDeprecatedOrExperimentalOrObsoleteAttribute(EntityHandle, IAttributeNamedArgumentDecoder, bool, bool)"/>
        /// </summary>
        internal static bool IsMoreImportantObsoleteKind(ObsoleteAttributeKind firstKind, ObsoleteAttributeKind secondKind)
        {
            return getPriority(firstKind) <= getPriority(secondKind);

            static int getPriority(ObsoleteAttributeKind kind) => kind switch
            {
                ObsoleteAttributeKind.Deprecated => 0,
                ObsoleteAttributeKind.Obsolete => 1,
                ObsoleteAttributeKind.WindowsExperimental => 2,
                ObsoleteAttributeKind.Experimental => 3,
                ObsoleteAttributeKind.Uninitialized => 4,
                _ => throw ExceptionUtilities.UnexpectedValue(kind)
            };
        }

        internal ObsoleteAttributeData? TryDecodeExperimentalAttributeData(EntityHandle handle, IAttributeNamedArgumentDecoder decoder)
        {
            var info = FindTargetAttribute(handle, AttributeDescription.ExperimentalAttribute);
            return info.HasValue ? TryExtractExperimentalDataFromAttribute(info, decoder) : null;
        }

        private ObsoleteAttributeData? TryExtractExperimentalDataFromAttribute(AttributeInfo attributeInfo, IAttributeNamedArgumentDecoder decoder)
        {
            Debug.Assert(attributeInfo.HasValue);
            if (!TryGetAttributeReader(attributeInfo.Handle, out var sig))
            {
                return null;
            }

            if (attributeInfo.SignatureIndex != 0)
            {
                throw ExceptionUtilities.UnexpectedValue(attributeInfo.SignatureIndex);
            }

            // ExperimentalAttribute(string)
            if (sig.RemainingBytes <= 0 || !CrackStringInAttributeValue(out string? diagnosticId, ref sig))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(diagnosticId))
            {
                diagnosticId = null;
            }

            string? urlFormat = crackUrlFormat(decoder, ref sig);
            return new ObsoleteAttributeData(ObsoleteAttributeKind.Experimental, message: null, isError: false, diagnosticId, urlFormat);

            static string? crackUrlFormat(IAttributeNamedArgumentDecoder decoder, ref BlobReader sig)
            {
                if (sig.RemainingBytes <= 0)
                {
                    return null;
                }

                string? urlFormat = null;

                try
                {
                    // See CIL spec section II.23.3 Custom attributes
                    //
                    // Next is a description of the optional “named” fields and properties.
                    // This starts with NumNamed– an unsigned int16 giving the number of “named” properties or fields that follow.
                    var numNamed = sig.ReadUInt16();
                    for (int i = 0; i < numNamed && urlFormat is null; i++)
                    {
                        var ((name, value), isProperty, typeCode, /* elementTypeCode */ _) = decoder.DecodeCustomAttributeNamedArgumentOrThrow(ref sig);
                        if (typeCode == SerializationTypeCode.String && isProperty && value.ValueInternal is string stringValue)
                        {
                            if (urlFormat is null && name == ObsoleteAttributeData.UrlFormatPropertyName)
                            {
                                urlFormat = stringValue;
                            }
                        }
                    }
                }
                catch (BadImageFormatException) { }
                catch (UnsupportedSignatureContent) { }

                return urlFormat;
            }
        }

        internal string? GetFirstUnsupportedCompilerFeatureFromToken(EntityHandle token, IAttributeNamedArgumentDecoder attributeNamedArgumentDecoder, CompilerFeatureRequiredFeatures allowedFeatures)
        {
            List<AttributeInfo>? infos = FindTargetAttributes(token, AttributeDescription.CompilerFeatureRequiredAttribute);

            if (infos == null)
            {
                return null;
            }

            foreach (var info in infos)
            {
                if (!info.HasValue || !TryGetAttributeReader(info.Handle, out BlobReader sigReader) || !CrackStringInAttributeValue(out string? featureName, ref sigReader))
                {
                    continue;
                }

                bool isOptional = false;
                if (sigReader.RemainingBytes >= 2)
                {
                    try
                    {
                        var numNamedArgs = sigReader.ReadUInt16();
                        for (uint i = 0; i < numNamedArgs; i++)
                        {
                            (KeyValuePair<string, TypedConstant> nameValuePair, bool isProperty, SerializationTypeCode typeCode, SerializationTypeCode elementTypeCode) namedArgValues =
                                attributeNamedArgumentDecoder.DecodeCustomAttributeNamedArgumentOrThrow(ref sigReader);

                            if (namedArgValues is ({ Key: "IsOptional" }, isProperty: true, typeCode: SerializationTypeCode.Boolean, _))
                            {
                                isOptional = (bool)namedArgValues.nameValuePair.Value.ValueInternal!;
                                break;
                            }
                        }
                    }
                    catch (Exception e) when (e is UnsupportedSignatureContent or BadImageFormatException) { }
                }

                if (!isOptional && (allowedFeatures & getFeatureKind(featureName)) == 0)
                {
                    return featureName;
                }
            }

            return null;

            static CompilerFeatureRequiredFeatures getFeatureKind(string? feature)
                => feature switch
                {
                    nameof(CompilerFeatureRequiredFeatures.RefStructs) => CompilerFeatureRequiredFeatures.RefStructs,
                    nameof(CompilerFeatureRequiredFeatures.RequiredMembers) => CompilerFeatureRequiredFeatures.RequiredMembers,
                    _ => CompilerFeatureRequiredFeatures.None,
                };
        }

        internal UnmanagedCallersOnlyAttributeData? TryGetUnmanagedCallersOnlyAttribute(
            EntityHandle token,
            IAttributeNamedArgumentDecoder attributeArgumentDecoder,
            Func<string, TypedConstant, bool, (bool IsCallConvs, ImmutableHashSet<INamedTypeSymbolInternal>? CallConvs)> unmanagedCallersOnlyDecoder)
        {
            // We don't want to load all attributes and their public data just to answer whether a PEMethodSymbol has an UnmanagedCallersOnly
            // attached. It would create unnecessary memory pressure that isn't going to be needed 99% of the time, so we just crack this 1
            // attribute.
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.UnmanagedCallersOnlyAttribute);
            if (!info.HasValue || info.SignatureIndex != 0 || !TryGetAttributeReader(info.Handle, out BlobReader sigReader))
            {
                return null;
            }

            var unmanagedConventionTypes = ImmutableHashSet<INamedTypeSymbolInternal>.Empty;

            if (sigReader.RemainingBytes > 0)
            {
                try
                {
                    var numNamed = sigReader.ReadUInt16();
                    for (int i = 0; i < numNamed; i++)
                    {
                        var ((name, value), isProperty, typeCode, elementTypeCode) = attributeArgumentDecoder.DecodeCustomAttributeNamedArgumentOrThrow(ref sigReader);
                        if (typeCode != SerializationTypeCode.SZArray || elementTypeCode != SerializationTypeCode.Type)
                        {
                            continue;
                        }

                        var namedArgumentDecoded = unmanagedCallersOnlyDecoder(name, value, !isProperty);
                        if (namedArgumentDecoded.IsCallConvs)
                        {
                            unmanagedConventionTypes = namedArgumentDecoded.CallConvs;
                            break;
                        }
                    }
                }
                catch (Exception ex) when (ex is BadImageFormatException or UnsupportedSignatureContent)
                {
                }
            }

            return UnmanagedCallersOnlyAttributeData.Create(unmanagedConventionTypes);
        }

        internal (ImmutableArray<string?> Names, bool FoundAttribute) GetInterpolatedStringHandlerArgumentAttributeValues(EntityHandle token)
        {
            var targetAttribute = FindTargetAttribute(token, AttributeDescription.InterpolatedStringHandlerArgumentAttribute);
            if (!targetAttribute.HasValue)
            {
                return (default, false);
            }

            Debug.Assert(AttributeDescription.InterpolatedStringHandlerArgumentAttribute.Signatures.Length == 2);
            Debug.Assert(targetAttribute.SignatureIndex is 0 or 1);
            if (targetAttribute.SignatureIndex == 0)
            {
                if (TryExtractStringValueFromAttribute(targetAttribute.Handle, out string? paramName))
                {
                    return (ImmutableArray.Create(paramName), true);
                }
            }
            else if (TryExtractStringArrayValueFromAttribute(targetAttribute.Handle, out var paramNames))
            {
                Debug.Assert(!paramNames.IsDefault);
                return (paramNames.NullToEmpty(), true);
            }

            return (default, true);
        }
#nullable disable

        internal bool HasMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(EntityHandle token, AttributeDescription description, out bool when)
        {
            Debug.Assert(description.Namespace == "System.Diagnostics.CodeAnalysis");
            Debug.Assert(description.Name == "MaybeNullWhenAttribute" || description.Name == "NotNullWhenAttribute" || description.Name == "DoesNotReturnIfAttribute");

            AttributeInfo info = FindTargetAttribute(token, description);
            if (info.HasValue &&
                // MaybeNullWhen(bool), NotNullWhen(bool), DoesNotReturnIf(bool)
                info.SignatureIndex == 0)
            {
                return TryExtractValueFromAttribute(info.Handle, out when, s_attributeBooleanValueExtractor);
            }
            when = false;
            return false;
        }

        internal ImmutableHashSet<string> GetStringValuesOfNotNullIfNotNullAttribute(EntityHandle token)
        {
            var attributeInfos = FindTargetAttributes(token, AttributeDescription.NotNullIfNotNullAttribute);

            var result = ImmutableHashSet<string>.Empty;
            if (attributeInfos is null)
            {
                return result;
            }

            foreach (var attributeInfo in attributeInfos)
            {
                if (TryExtractStringValueFromAttribute(attributeInfo.Handle, out string parameterName))
                {
                    result = result.Add(parameterName);
                }
            }

            return result;
        }

        internal bool HasAttributeUsageAttribute(EntityHandle token, IAttributeNamedArgumentDecoder attributeNamedArgumentDecoder, out AttributeUsageInfo usageInfo)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.AttributeUsageAttribute);

            if (info.HasValue)
            {
                Debug.Assert(info.SignatureIndex == 0);
                if (TryGetAttributeReader(info.Handle, out BlobReader sigReader) && CrackIntInAttributeValue(out int validOn, ref sigReader))
                {
                    bool allowMultiple = false;
                    bool inherited = true;

                    if (sigReader.RemainingBytes >= 2)
                    {
                        try
                        {
                            var numNamedArgs = sigReader.ReadUInt16();
                            for (uint i = 0; i < numNamedArgs; i++)
                            {
                                (KeyValuePair<string, TypedConstant> nameValuePair, bool isProperty, SerializationTypeCode typeCode, SerializationTypeCode elementTypeCode) namedArgValues =
                                    attributeNamedArgumentDecoder.DecodeCustomAttributeNamedArgumentOrThrow(ref sigReader);

                                if (namedArgValues is (_, isProperty: true, typeCode: SerializationTypeCode.Boolean, _))
                                {
                                    switch (namedArgValues.nameValuePair.Key)
                                    {
                                        case "AllowMultiple":
                                            allowMultiple = (bool)namedArgValues.nameValuePair.Value.ValueInternal!;
                                            break;
                                        case "Inherited":
                                            inherited = (bool)namedArgValues.nameValuePair.Value.ValueInternal!;
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception e) when (e is UnsupportedSignatureContent or BadImageFormatException) { }
                    }

                    usageInfo = new AttributeUsageInfo((AttributeTargets)validOn, allowMultiple, inherited);
                    return true;
                }
            }

            usageInfo = default;
            return false;
        }

        internal bool HasInterfaceTypeAttribute(EntityHandle token, out ComInterfaceType interfaceType)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.InterfaceTypeAttribute);
            if (info.HasValue && TryExtractInterfaceTypeFromAttribute(info, out interfaceType))
            {
                return true;
            }

            interfaceType = default(ComInterfaceType);
            return false;
        }

        internal bool HasTypeLibTypeAttribute(EntityHandle token, out Cci.TypeLibTypeFlags flags)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.TypeLibTypeAttribute);
            if (info.HasValue && TryExtractTypeLibTypeFromAttribute(info, out flags))
            {
                return true;
            }

            flags = default(Cci.TypeLibTypeFlags);
            return false;
        }

        internal bool HasDateTimeConstantAttribute(EntityHandle token, out ConstantValue defaultValue)
        {
            long value;
            AttributeInfo info = FindLastTargetAttribute(token, AttributeDescription.DateTimeConstantAttribute);
            if (info.HasValue && TryExtractLongValueFromAttribute(info.Handle, out value))
            {
                // if value is outside this range, DateTime would throw when constructed
                if (value < DateTime.MinValue.Ticks || value > DateTime.MaxValue.Ticks)
                {
                    defaultValue = ConstantValue.Bad;
                }
                else
                {
                    defaultValue = ConstantValue.Create(new DateTime(value));
                }

                return true;
            }

            defaultValue = null;
            return false;
        }

        internal bool HasDecimalConstantAttribute(EntityHandle token, out ConstantValue defaultValue)
        {
            decimal value;
            AttributeInfo info = FindLastTargetAttribute(token, AttributeDescription.DecimalConstantAttribute);
            if (info.HasValue && TryExtractDecimalValueFromDecimalConstantAttribute(info.Handle, out value))
            {
                defaultValue = ConstantValue.Create(value);
                return true;
            }

            defaultValue = null;
            return false;
        }

        internal bool HasNullablePublicOnlyAttribute(EntityHandle token, out bool includesInternals)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.NullablePublicOnlyAttribute);
            if (info.HasValue)
            {
                Debug.Assert(info.SignatureIndex == 0);
                if (TryExtractValueFromAttribute(info.Handle, out bool value, s_attributeBooleanValueExtractor))
                {
                    includesInternals = value;
                    return true;
                }
            }
            includesInternals = false;
            return false;
        }

        internal ImmutableArray<string> GetInternalsVisibleToAttributeValues(EntityHandle token)
        {
            List<AttributeInfo> attrInfos = FindTargetAttributes(token, AttributeDescription.InternalsVisibleToAttribute);
            ArrayBuilder<string> result = ExtractStringValuesFromAttributes(attrInfos);
            return result?.ToImmutableAndFree() ?? ImmutableArray<string>.Empty;
        }

        internal ImmutableArray<string> GetConditionalAttributeValues(EntityHandle token)
        {
            List<AttributeInfo> attrInfos = FindTargetAttributes(token, AttributeDescription.ConditionalAttribute);
            ArrayBuilder<string> result = ExtractStringValuesFromAttributes(attrInfos);
            return result?.ToImmutableAndFree() ?? ImmutableArray<string>.Empty;
        }

        /// <summary>
        /// Find the MemberNotNull attribute(s) and extract the list of referenced member names
        /// </summary>
        internal ImmutableArray<string> GetMemberNotNullAttributeValues(EntityHandle token)
        {
            List<AttributeInfo> attrInfos = FindTargetAttributes(token, AttributeDescription.MemberNotNullAttribute);
            if (attrInfos is null || attrInfos.Count == 0)
            {
                return ImmutableArray<string>.Empty;
            }

            var result = ArrayBuilder<string>.GetInstance(attrInfos.Count);

            foreach (var ai in attrInfos)
            {
                if (ai.SignatureIndex == 0)
                {
                    if (TryExtractStringValueFromAttribute(ai.Handle, out string extracted))
                    {
                        if (extracted is object)
                        {
                            result.Add(extracted);
                        }
                    }
                }
                else if (TryExtractStringArrayValueFromAttribute(ai.Handle, out ImmutableArray<string> extracted2))
                {
                    foreach (var value in extracted2)
                    {
                        if (value is object)
                        {
                            result.Add(value);
                        }
                    }
                }
            }

            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Find the MemberNotNullWhen attribute(s) and extract the list of referenced member names
        /// </summary>
        internal (ImmutableArray<string> whenTrue, ImmutableArray<string> whenFalse) GetMemberNotNullWhenAttributeValues(EntityHandle token)
        {
            List<AttributeInfo> attrInfos = FindTargetAttributes(token, AttributeDescription.MemberNotNullWhenAttribute);
            if (attrInfos is null || attrInfos.Count == 0)
            {
                return (ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);
            }

            var whenTrue = ArrayBuilder<string>.GetInstance(attrInfos.Count);
            var whenFalse = ArrayBuilder<string>.GetInstance(attrInfos.Count);

            foreach (var ai in attrInfos)
            {
                if (ai.SignatureIndex == 0)
                {
                    if (TryExtractValueFromAttribute(ai.Handle, out BoolAndStringData extracted, s_attributeBoolAndStringValueExtractor))
                    {
                        if (extracted.String is object)
                        {
                            var whenResult = extracted.Sense ? whenTrue : whenFalse;
                            whenResult.Add(extracted.String);
                        }
                    }
                }
                else if (TryExtractValueFromAttribute(ai.Handle, out BoolAndStringArrayData extracted2, s_attributeBoolAndStringArrayValueExtractor))
                {
                    var whenResult = extracted2.Sense ? whenTrue : whenFalse;
                    foreach (var value in extracted2.Strings)
                    {
                        if (value is object)
                        {
                            whenResult.Add(value);
                        }
                    }
                }
            }

            return (whenTrue.ToImmutableAndFree(), whenFalse.ToImmutableAndFree());
        }

        // This method extracts all the non-null string values from the given attributes.
        private ArrayBuilder<string> ExtractStringValuesFromAttributes(List<AttributeInfo> attrInfos)
        {
            if (attrInfos == null)
            {
                return null;
            }

            var result = ArrayBuilder<string>.GetInstance(attrInfos.Count);

            foreach (var ai in attrInfos)
            {
                string extractedStr;
                if (TryExtractStringValueFromAttribute(ai.Handle, out extractedStr) && extractedStr != null)
                {
                    result.Add(extractedStr);
                }
            }

            return result;
        }

#nullable enable
        private ObsoleteAttributeData? TryExtractObsoleteDataFromAttribute(AttributeInfo attributeInfo, IAttributeNamedArgumentDecoder decoder)
        {
            Debug.Assert(attributeInfo.HasValue);
            if (!TryGetAttributeReader(attributeInfo.Handle, out var sig))
            {
                return null;
            }

            string? message = null;
            bool isError = false;
            switch (attributeInfo.SignatureIndex)
            {
                case 0:
                    // ObsoleteAttribute()
                    break;
                case 1:
                    // ObsoleteAttribute(string)
                    if (sig.RemainingBytes > 0 && CrackStringInAttributeValue(out message, ref sig))
                    {
                        break;
                    }

                    return null;
                case 2:
                    // ObsoleteAttribute(string, bool)
                    if (sig.RemainingBytes > 0 && CrackStringInAttributeValue(out message, ref sig) &&
                        sig.RemainingBytes > 0 && CrackBooleanInAttributeValue(out isError, ref sig))
                    {
                        break;
                    }

                    return null;
                default:
                    throw ExceptionUtilities.UnexpectedValue(attributeInfo.SignatureIndex);
            }

            (string? diagnosticId, string? urlFormat) = sig.RemainingBytes > 0 ? CrackObsoleteProperties(ref sig, decoder) : default;
            return new ObsoleteAttributeData(ObsoleteAttributeKind.Obsolete, message, isError, diagnosticId, urlFormat);
        }

        private bool TryGetAttributeReader(CustomAttributeHandle handle, out BlobReader blobReader)
        {
            Debug.Assert(!handle.IsNil);
            try
            {
                var valueBlob = GetCustomAttributeValueOrThrow(handle);
                if (!valueBlob.IsNil)
                {
                    blobReader = MetadataReader.GetBlobReader(valueBlob);
                    if (blobReader.Length >= 4)
                    {
                        // check prolog
                        if (blobReader.ReadInt16() == 1)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            blobReader = default;
            return false;
        }
#nullable disable

        private ObsoleteAttributeData TryExtractDeprecatedDataFromAttribute(AttributeInfo attributeInfo)
        {
            Debug.Assert(attributeInfo.HasValue);

            switch (attributeInfo.SignatureIndex)
            {
                case 0: // DeprecatedAttribute(String, DeprecationType, UInt32) 
                case 1: // DeprecatedAttribute(String, DeprecationType, UInt32, Platform) 
                case 2: // DeprecatedAttribute(String, DeprecationType, UInt32, Type) 
                case 3: // DeprecatedAttribute(String, DeprecationType, UInt32, String) 
                    return TryExtractValueFromAttribute(attributeInfo.Handle, out var obsoleteData, s_attributeDeprecatedDataExtractor) ?
                        obsoleteData :
                        null;

                default:
                    throw ExceptionUtilities.UnexpectedValue(attributeInfo.SignatureIndex);
            }
        }

        private ObsoleteAttributeData TryExtractWindowsExperimentalDataFromAttribute(AttributeInfo attributeInfo)
        {
            Debug.Assert(attributeInfo.HasValue);

            switch (attributeInfo.SignatureIndex)
            {
                case 0: // ExperimentalAttribute() 
                    return ObsoleteAttributeData.WindowsExperimental;

                default:
                    throw ExceptionUtilities.UnexpectedValue(attributeInfo.SignatureIndex);
            }
        }

        private bool TryExtractInterfaceTypeFromAttribute(AttributeInfo attributeInfo, out ComInterfaceType interfaceType)
        {
            Debug.Assert(attributeInfo.HasValue);

            switch (attributeInfo.SignatureIndex)
            {
                case 0:
                    // InterfaceTypeAttribute(Int16)
                    short shortValue;
                    if (TryExtractValueFromAttribute(attributeInfo.Handle, out shortValue, s_attributeShortValueExtractor) &&
                        IsValidComInterfaceType(shortValue))
                    {
                        interfaceType = (ComInterfaceType)shortValue;
                        return true;
                    }
                    break;

                case 1:
                    // InterfaceTypeAttribute(ComInterfaceType)
                    int intValue;
                    if (TryExtractValueFromAttribute(attributeInfo.Handle, out intValue, s_attributeIntValueExtractor) &&
                        IsValidComInterfaceType(intValue))
                    {
                        interfaceType = (ComInterfaceType)intValue;
                        return true;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(attributeInfo.SignatureIndex);
            }

            interfaceType = default(ComInterfaceType);
            return false;
        }

        private static bool IsValidComInterfaceType(int comInterfaceType)
        {
            switch (comInterfaceType)
            {
                case (int)Cci.Constants.ComInterfaceType_InterfaceIsDual:
                case (int)Cci.Constants.ComInterfaceType_InterfaceIsIDispatch:
                case (int)ComInterfaceType.InterfaceIsIInspectable:
                case (int)ComInterfaceType.InterfaceIsIUnknown:
                    return true;

                default:
                    return false;
            }
        }

        private bool TryExtractTypeLibTypeFromAttribute(AttributeInfo info, out Cci.TypeLibTypeFlags flags)
        {
            Debug.Assert(info.HasValue);

            switch (info.SignatureIndex)
            {
                case 0:
                    // TypeLibTypeAttribute(Int16)
                    short shortValue;
                    if (TryExtractValueFromAttribute(info.Handle, out shortValue, s_attributeShortValueExtractor))
                    {
                        flags = (Cci.TypeLibTypeFlags)shortValue;
                        return true;
                    }
                    break;

                case 1:
                    // TypeLibTypeAttribute(TypeLibTypeFlags)
                    int intValue;
                    if (TryExtractValueFromAttribute(info.Handle, out intValue, s_attributeIntValueExtractor))
                    {
                        flags = (Cci.TypeLibTypeFlags)intValue;
                        return true;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(info.SignatureIndex);
            }

            flags = default(Cci.TypeLibTypeFlags);
            return false;
        }

#nullable enable
        internal bool TryExtractStringValueFromAttribute(CustomAttributeHandle handle, out string? value)
        {
            return TryExtractValueFromAttribute(handle, out value, s_attributeStringValueExtractor);
        }

        internal bool TryExtractLongValueFromAttribute(CustomAttributeHandle handle, out long value)
        {
            return TryExtractValueFromAttribute(handle, out value, s_attributeLongValueExtractor);
        }

        // Note: not a general purpose helper
        private bool TryExtractDecimalValueFromDecimalConstantAttribute(CustomAttributeHandle handle, out decimal value)
        {
            return TryExtractValueFromAttribute(handle, out value, s_decimalValueInDecimalConstantAttributeExtractor);
        }

        private bool TryExtractIntAndIntValueFromAttribute(CustomAttributeHandle handle, out int value1, out int value2)
        {
            bool result = TryExtractValueFromAttribute(handle, out (int, int) data, s_attributeIntAndIntValueExtractor);
            (value1, value2) = data;
            return result;
        }

        private struct StringAndInt
        {
            public string? StringValue;
            public int IntValue;
        }

        private bool TryExtractStringAndIntValueFromAttribute(CustomAttributeHandle handle, out string? stringValue, out int intValue)
        {
            StringAndInt data;
            var result = TryExtractValueFromAttribute(handle, out data, s_attributeStringAndIntValueExtractor);
            stringValue = data.StringValue;
            intValue = data.IntValue;
            return result;
        }

        private bool TryExtractStringAndStringValueFromAttribute(CustomAttributeHandle handle, out string? string1Value, out string? string2Value)
        {
            (string?, string?) data;
            var result = TryExtractValueFromAttribute(handle, out data, s_attributeStringAndStringValueExtractor);
            (string1Value, string2Value) = data;
            return result;
        }

        private bool TryExtractBoolArrayValueFromAttribute(CustomAttributeHandle handle, out ImmutableArray<bool> value)
        {
            return TryExtractValueFromAttribute(handle, out value, s_attributeBoolArrayValueExtractor);
        }

        private bool TryExtractByteArrayValueFromAttribute(CustomAttributeHandle handle, out ImmutableArray<byte> value)
        {
            return TryExtractValueFromAttribute(handle, out value, s_attributeByteArrayValueExtractor);
        }

        private bool TryExtractStringArrayValueFromAttribute(CustomAttributeHandle handle, out ImmutableArray<string?> value)
        {
            return TryExtractValueFromAttribute(handle, out value, s_attributeStringArrayValueExtractor);
        }

        private bool TryExtractValueFromAttribute<T>(CustomAttributeHandle handle, out T? value, AttributeValueExtractor<T?> valueExtractor)
        {
            Debug.Assert(!handle.IsNil);

            // extract the value
            try
            {
                BlobHandle valueBlob = GetCustomAttributeValueOrThrow(handle);

                if (!valueBlob.IsNil)
                {
                    // TODO: error checking offset in range
                    BlobReader reader = MetadataReader.GetBlobReader(valueBlob);

                    if (reader.Length > 4)
                    {
                        // check prolog
                        if (reader.ReadByte() == 1 && reader.ReadByte() == 0)
                        {
                            return valueExtractor(out value, ref reader);
                        }
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            value = default(T);
            return false;
        }
#nullable disable

        internal bool HasStateMachineAttribute(MethodDefinitionHandle handle, out string stateMachineTypeName)
            => HasStringValuedAttribute(handle, AttributeDescription.AsyncStateMachineAttribute, out stateMachineTypeName) ||
               HasStringValuedAttribute(handle, AttributeDescription.IteratorStateMachineAttribute, out stateMachineTypeName) ||
               HasStringValuedAttribute(handle, AttributeDescription.AsyncIteratorStateMachineAttribute, out stateMachineTypeName);

        internal bool HasStringValuedAttribute(EntityHandle token, AttributeDescription description, out string value)
        {
            AttributeInfo info = FindTargetAttribute(token, description);
            if (info.HasValue)
            {
                return TryExtractStringValueFromAttribute(info.Handle, out value);
            }

            value = null;
            return false;
        }

        private bool HasIntAndIntValuedAttribute(EntityHandle token, AttributeDescription description, out int value1, out int value2)
        {
            AttributeInfo info = FindTargetAttribute(token, description);
            if (info.HasValue)
            {
                return TryExtractIntAndIntValueFromAttribute(info.Handle, out value1, out value2);
            }

            value1 = 0;
            value2 = 0;
            return false;
        }

        private bool HasStringAndIntValuedAttribute(EntityHandle token, AttributeDescription description, out string stringValue, out int intValue)
        {
            AttributeInfo info = FindTargetAttribute(token, description);
            if (info.HasValue)
            {
                return TryExtractStringAndIntValueFromAttribute(info.Handle, out stringValue, out intValue);
            }

            stringValue = null;
            intValue = 0;
            return false;
        }

        internal bool IsNoPiaLocalType(
            TypeDefinitionHandle typeDef,
            out string interfaceGuid,
            out string scope,
            out string identifier)
        {
            AttributeInfo typeIdentifierInfo;

            if (!IsNoPiaLocalType(typeDef, out typeIdentifierInfo))
            {
                interfaceGuid = null;
                scope = null;
                identifier = null;

                return false;
            }

            interfaceGuid = null;
            scope = null;
            identifier = null;

            try
            {
                if (GetTypeDefFlagsOrThrow(typeDef).IsInterface())
                {
                    HasGuidAttribute(typeDef, out interfaceGuid);
                }

                if (typeIdentifierInfo.SignatureIndex == 1)
                {
                    // extract the value
                    BlobHandle valueBlob = GetCustomAttributeValueOrThrow(typeIdentifierInfo.Handle);

                    if (!valueBlob.IsNil)
                    {
                        BlobReader reader = MetadataReader.GetBlobReader(valueBlob);

                        if (reader.Length > 4)
                        {
                            // check prolog
                            if (reader.ReadInt16() == 1)
                            {
                                if (!CrackStringInAttributeValue(out scope, ref reader) ||
                                    !CrackStringInAttributeValue(out identifier, ref reader))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

#nullable enable
        /// <summary>
        /// Gets the well-known optional named properties on ObsoleteAttribute, if present.
        /// Both 'diagnosticId' and 'urlFormat' may be present, or only one, or neither.
        /// </summary>
        /// <remarks>
        /// Failure to find any of these properties does not imply failure to decode the ObsoleteAttribute,
        /// so we don't return a value indicating success or failure.
        /// </remarks>
        private static (string? diagnosticId, string? urlFormat) CrackObsoleteProperties(ref BlobReader sig, IAttributeNamedArgumentDecoder decoder)
        {
            string? diagnosticId = null;
            string? urlFormat = null;

            try
            {
                // See CIL spec section II.23.3 Custom attributes
                //
                // Next is a description of the optional “named” fields and properties.
                // This starts with NumNamed– an unsigned int16 giving the number of “named” properties or fields that follow.
                var numNamed = sig.ReadUInt16();
                for (int i = 0; i < numNamed && (diagnosticId is null || urlFormat is null); i++)
                {
                    var ((name, value), isProperty, typeCode, /* elementTypeCode */ _) = decoder.DecodeCustomAttributeNamedArgumentOrThrow(ref sig);
                    if (typeCode == SerializationTypeCode.String && isProperty && value.ValueInternal is string stringValue)
                    {
                        if (diagnosticId is null && name == ObsoleteAttributeData.DiagnosticIdPropertyName)
                        {
                            diagnosticId = stringValue;
                        }
                        else if (urlFormat is null && name == ObsoleteAttributeData.UrlFormatPropertyName)
                        {
                            urlFormat = stringValue;
                        }
                    }
                }
            }
            catch (BadImageFormatException) { }
            catch (UnsupportedSignatureContent) { }

            return (diagnosticId, urlFormat);
        }

        private static bool CrackDeprecatedAttributeData([NotNullWhen(true)] out ObsoleteAttributeData? value, ref BlobReader sig)
        {
            StringAndInt args;
            if (CrackStringAndIntInAttributeValue(out args, ref sig))
            {
                value = new ObsoleteAttributeData(ObsoleteAttributeKind.Deprecated, args.StringValue, args.IntValue == 1, diagnosticId: null, urlFormat: null);
                return true;
            }

            value = null;
            return false;
        }

        private static bool CrackIntAndIntInAttributeValue(out (int, int) value, ref BlobReader sig)
        {
            if (CrackIntInAttributeValue(out int value1, ref sig) &&
                CrackIntInAttributeValue(out int value2, ref sig))
            {
                value = (value1, value2);
                return true;
            }

            value = default;
            return false;
        }

        private static bool CrackStringAndIntInAttributeValue(out StringAndInt value, ref BlobReader sig)
        {
            value = default(StringAndInt);
            return
                CrackStringInAttributeValue(out value.StringValue, ref sig) &&
                CrackIntInAttributeValue(out value.IntValue, ref sig);
        }

        private static bool CrackStringAndStringInAttributeValue(out (string?, string?) value, ref BlobReader sig)
        {
            if (CrackStringInAttributeValue(out string? string1, ref sig) &&
                CrackStringInAttributeValue(out string? string2, ref sig))
            {
                value = (string1, string2);
                return true;
            }

            value = default;
            return false;
        }

        internal static bool CrackStringInAttributeValue(out string? value, ref BlobReader sig)
        {
            try
            {
                int strLen;
                if (sig.TryReadCompressedInteger(out strLen) && sig.RemainingBytes >= strLen)
                {
                    value = sig.ReadUTF8(strLen);

                    // Trim null characters at the end to mimic native compiler behavior.
                    // There are libraries that have them and leaving them in breaks tests.
                    value = value.TrimEnd('\0');

                    return true;
                }

                value = null;

                // Strings are stored as UTF-8, but 0xFF means NULL string.
                return sig.RemainingBytes >= 1 && sig.ReadByte() == 0xFF;
            }
            catch (BadImageFormatException)
            {
                value = null;
                return false;
            }
        }

        internal static bool CrackStringArrayInAttributeValue(out ImmutableArray<string?> value, ref BlobReader sig)
        {
            if (sig.RemainingBytes >= 4)
            {
                uint arrayLen = sig.ReadUInt32();

                if (IsArrayNull(arrayLen))
                {
                    value = default;
                    return false;
                }

                var stringArray = new string?[arrayLen];

                for (int i = 0; i < arrayLen; i++)
                {
                    if (!CrackStringInAttributeValue(out stringArray[i], ref sig))
                    {
                        value = stringArray.AsImmutableOrNull();
                        return false;
                    }
                }

                value = stringArray.AsImmutableOrNull();
                return true;
            }

            value = default;
            return false;
        }

        private static bool IsArrayNull(uint length)
        {
            // Null arrays are represented in metadata by a length of 0xFFFF_FFFF. See ECMA 335 II.23.3.
            const uint NullArray = 0xFFFF_FFFF;

            if (length == NullArray)
            {
                return true;
            }

            return false;
        }

        private static bool CrackBoolAndStringArrayInAttributeValue(out BoolAndStringArrayData value, ref BlobReader sig)
        {
            if (CrackBooleanInAttributeValue(out bool sense, ref sig) &&
                CrackStringArrayInAttributeValue(out ImmutableArray<string?> strings, ref sig))
            {
                value = new BoolAndStringArrayData(sense, strings);
                return true;
            }

            value = default;
            return false;
        }

        private static bool CrackBoolAndStringInAttributeValue(out BoolAndStringData value, ref BlobReader sig)
        {
            if (CrackBooleanInAttributeValue(out bool sense, ref sig) &&
                CrackStringInAttributeValue(out string? @string, ref sig))
            {
                value = new BoolAndStringData(sense, @string);
                return true;
            }

            value = default;
            return false;
        }

        private static bool CrackBoolAndBoolInAttributeValue(out (bool, bool) value, ref BlobReader sig)
        {
            if (CrackBooleanInAttributeValue(out bool item1, ref sig) &&
                CrackBooleanInAttributeValue(out bool item2, ref sig))
            {
                value = (item1, item2);
                return true;
            }

            value = default;
            return false;
        }

        private static bool CrackBooleanInAttributeValue(out bool value, ref BlobReader sig)
        {
            if (sig.RemainingBytes >= 1)
            {
                value = sig.ReadBoolean();
                return true;
            }

            value = false;
            return false;
        }

        private static bool CrackByteInAttributeValue(out byte value, ref BlobReader sig)
        {
            if (sig.RemainingBytes >= 1)
            {
                value = sig.ReadByte();
                return true;
            }

            value = 0xff;
            return false;
        }

        private static bool CrackShortInAttributeValue(out short value, ref BlobReader sig)
        {
            if (sig.RemainingBytes >= 2)
            {
                value = sig.ReadInt16();
                return true;
            }

            value = -1;
            return false;
        }

        private static bool CrackIntInAttributeValue(out int value, ref BlobReader sig)
        {
            if (sig.RemainingBytes >= 4)
            {
                value = sig.ReadInt32();
                return true;
            }

            value = -1;
            return false;
        }

        private static bool CrackLongInAttributeValue(out long value, ref BlobReader sig)
        {
            if (sig.RemainingBytes >= 8)
            {
                value = sig.ReadInt64();
                return true;
            }

            value = -1;
            return false;
        }

        // Note: not a general purpose helper
        private static bool CrackDecimalInDecimalConstantAttribute(out decimal value, ref BlobReader sig)
        {
            byte scale;
            byte sign;
            int high;
            int mid;
            int low;

            if (CrackByteInAttributeValue(out scale, ref sig) &&
                CrackByteInAttributeValue(out sign, ref sig) &&
                CrackIntInAttributeValue(out high, ref sig) &&
                CrackIntInAttributeValue(out mid, ref sig) &&
                CrackIntInAttributeValue(out low, ref sig))
            {
                value = new decimal(low, mid, high, sign != 0, scale);
                return true;
            }

            value = -1;
            return false;
        }

        private static bool CrackBoolArrayInAttributeValue(out ImmutableArray<bool> value, ref BlobReader sig)
        {
            if (sig.RemainingBytes >= 4)
            {
                uint arrayLen = sig.ReadUInt32();

                if (IsArrayNull(arrayLen))
                {
                    value = default;
                    return false;
                }

                if (sig.RemainingBytes >= arrayLen)
                {
                    var boolArrayBuilder = ArrayBuilder<bool>.GetInstance((int)arrayLen);
                    for (int i = 0; i < arrayLen; i++)
                    {
                        boolArrayBuilder.Add(sig.ReadByte() == 1);
                    }

                    value = boolArrayBuilder.ToImmutableAndFree();
                    return true;
                }
            }

            value = default(ImmutableArray<bool>);
            return false;
        }

        private static bool CrackByteArrayInAttributeValue(out ImmutableArray<byte> value, ref BlobReader sig)
        {
            if (sig.RemainingBytes >= 4)
            {
                uint arrayLen = sig.ReadUInt32();

                if (IsArrayNull(arrayLen))
                {
                    value = default;
                    return false;
                }

                if (sig.RemainingBytes >= arrayLen)
                {
                    var byteArrayBuilder = ArrayBuilder<byte>.GetInstance((int)arrayLen);
                    for (int i = 0; i < arrayLen; i++)
                    {
                        byteArrayBuilder.Add(sig.ReadByte());
                    }

                    value = byteArrayBuilder.ToImmutableAndFree();
                    return true;
                }
            }

            value = default(ImmutableArray<byte>);
            return false;
        }
#nullable disable

        internal readonly struct AttributeInfo
        {
            public readonly CustomAttributeHandle Handle;
            public readonly byte SignatureIndex;

            public AttributeInfo(CustomAttributeHandle handle, int signatureIndex)
            {
                Debug.Assert(signatureIndex >= 0 && signatureIndex <= byte.MaxValue);
                this.Handle = handle;
                this.SignatureIndex = (byte)signatureIndex;
            }

            public bool HasValue
            {
                get { return !Handle.IsNil; }
            }
        }

#nullable enable
        internal List<AttributeInfo>? FindTargetAttributes(EntityHandle hasAttribute, AttributeDescription description)
        {
            List<AttributeInfo>? result = null;

            try
            {
                foreach (var attributeHandle in MetadataReader.GetCustomAttributes(hasAttribute))
                {
                    int signatureIndex = GetTargetAttributeSignatureIndex(attributeHandle, description);
                    if (signatureIndex != -1)
                    {
                        if (result == null)
                        {
                            result = new List<AttributeInfo>();
                        }

                        // We found a match
                        result.Add(new AttributeInfo(attributeHandle, signatureIndex));
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            return result;
        }
#nullable disable

        internal AttributeInfo FindTargetAttribute(EntityHandle hasAttribute, AttributeDescription description)
        {
            return FindTargetAttribute(MetadataReader, hasAttribute, description, out _);
        }

        internal static AttributeInfo FindTargetAttribute(MetadataReader metadataReader, EntityHandle hasAttribute, AttributeDescription description, out bool foundAttributeType)
        {
            foundAttributeType = false;

            try
            {
                foreach (var attributeHandle in metadataReader.GetCustomAttributes(hasAttribute))
                {
                    bool matchedAttributeType;
                    int signatureIndex = GetTargetAttributeSignatureIndex(metadataReader, attributeHandle, description, out matchedAttributeType);
                    if (matchedAttributeType)
                    {
                        foundAttributeType = true;
                    }
                    if (signatureIndex != -1)
                    {
                        // We found a match
                        return new AttributeInfo(attributeHandle, signatureIndex);
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            return default(AttributeInfo);
        }

        internal AttributeInfo FindLastTargetAttribute(EntityHandle hasAttribute, AttributeDescription description)
        {
            try
            {
                AttributeInfo attrInfo = default(AttributeInfo);
                foreach (var attributeHandle in MetadataReader.GetCustomAttributes(hasAttribute))
                {
                    int signatureIndex = GetTargetAttributeSignatureIndex(attributeHandle, description);
                    if (signatureIndex != -1)
                    {
                        // We found a match
                        attrInfo = new AttributeInfo(attributeHandle, signatureIndex);
                    }
                }
                return attrInfo;
            }
            catch (BadImageFormatException)
            { }

            return default(AttributeInfo);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal int GetParamArrayCountOrThrow(EntityHandle hasAttribute)
        {
            int count = 0;
            foreach (var attributeHandle in MetadataReader.GetCustomAttributes(hasAttribute))
            {
                if (GetTargetAttributeSignatureIndex(attributeHandle,
                    AttributeDescription.ParamArrayAttribute) != -1)
                {
                    count++;
                }
            }
            return count;
        }

        private bool IsNoPiaLocalType(TypeDefinitionHandle typeDef, out AttributeInfo attributeInfo)
        {
            if (_lazyContainsNoPiaLocalTypes == ThreeState.False)
            {
                attributeInfo = default(AttributeInfo);
                return false;
            }

            if (_lazyNoPiaLocalTypeCheckBitMap != null &&
                _lazyTypeDefToTypeIdentifierMap != null)
            {
                int rid = MetadataReader.GetRowNumber(typeDef);
                Debug.Assert(rid > 0);

                int item = rid / 32;
                int bit = 1 << (rid % 32);

                if ((_lazyNoPiaLocalTypeCheckBitMap[item] & bit) != 0)
                {
                    return _lazyTypeDefToTypeIdentifierMap.TryGetValue(typeDef, out attributeInfo);
                }
            }

            try
            {
                foreach (var attributeHandle in MetadataReader.GetCustomAttributes(typeDef))
                {
                    int signatureIndex = IsTypeIdentifierAttribute(attributeHandle);
                    if (signatureIndex != -1)
                    {
                        // We found a match
                        _lazyContainsNoPiaLocalTypes = ThreeState.True;

                        RegisterNoPiaLocalType(typeDef, attributeHandle, signatureIndex);
                        attributeInfo = new AttributeInfo(attributeHandle, signatureIndex);
                        return true;
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            RecordNoPiaLocalTypeCheck(typeDef);
            attributeInfo = default(AttributeInfo);
            return false;
        }

        private void RegisterNoPiaLocalType(TypeDefinitionHandle typeDef, CustomAttributeHandle customAttribute, int signatureIndex)
        {
            if (_lazyNoPiaLocalTypeCheckBitMap == null)
            {
                Interlocked.CompareExchange(
                    ref _lazyNoPiaLocalTypeCheckBitMap,
                    new int[(MetadataReader.TypeDefinitions.Count + 32) / 32],
                    null);
            }

            if (_lazyTypeDefToTypeIdentifierMap == null)
            {
                Interlocked.CompareExchange(
                    ref _lazyTypeDefToTypeIdentifierMap,
                    new ConcurrentDictionary<TypeDefinitionHandle, AttributeInfo>(),
                    null);
            }

            _lazyTypeDefToTypeIdentifierMap.TryAdd(typeDef, new AttributeInfo(customAttribute, signatureIndex));

            RecordNoPiaLocalTypeCheck(typeDef);
        }

        private void RecordNoPiaLocalTypeCheck(TypeDefinitionHandle typeDef)
        {
            if (_lazyNoPiaLocalTypeCheckBitMap == null)
            {
                return;
            }

            int rid = MetadataTokens.GetRowNumber(typeDef);
            Debug.Assert(rid > 0);
            int item = rid / 32;
            int bit = 1 << (rid % 32);
            int oldValue;

            do
            {
                oldValue = _lazyNoPiaLocalTypeCheckBitMap[item];
            }
            while (Interlocked.CompareExchange(
                        ref _lazyNoPiaLocalTypeCheckBitMap[item],
                        oldValue | bit,
                        oldValue) != oldValue);
        }

        /// <summary>
        /// Determine if custom attribute application is 
        /// NoPia TypeIdentifier.
        /// </summary>
        /// <returns>
        /// An index of the target constructor signature in 
        /// signaturesOfTypeIdentifierAttribute array, -1 if
        /// this is not NoPia TypeIdentifier.
        /// </returns>
        private int IsTypeIdentifierAttribute(CustomAttributeHandle customAttribute)
        {
            const int No = -1;

            try
            {
                if (MetadataReader.GetCustomAttribute(customAttribute).Parent.Kind != HandleKind.TypeDefinition)
                {
                    // Ignore attributes attached to anything, but type definitions.
                    return No;
                }

                return GetTargetAttributeSignatureIndex(customAttribute, AttributeDescription.TypeIdentifierAttribute);
            }
            catch (BadImageFormatException)
            {
                return No;
            }
        }

        /// <summary>
        /// Determines if a custom attribute matches a namespace and name.
        /// </summary>
        /// <param name="customAttribute">Handle of the custom attribute.</param>
        /// <param name="namespaceName">The custom attribute's namespace in metadata format (case sensitive)</param>
        /// <param name="typeName">The custom attribute's type name in metadata format (case sensitive)</param>
        /// <param name="ctor">Constructor of the custom attribute.</param>
        /// <param name="ignoreCase">Should case be ignored for name comparison?</param>
        /// <returns>true if match is found</returns>
        internal bool IsTargetAttribute(
            CustomAttributeHandle customAttribute,
            string namespaceName,
            string typeName,
            out EntityHandle ctor,
            bool ignoreCase = false)
        {
            return IsTargetAttribute(MetadataReader, customAttribute, namespaceName, typeName, out ctor, ignoreCase);
        }

        /// <summary>
        /// Determines if a custom attribute matches a namespace and name.
        /// </summary>
        /// <param name="metadataReader">The metadata reader.</param>
        /// <param name="customAttribute">Handle of the custom attribute.</param>
        /// <param name="namespaceName">The custom attribute's namespace in metadata format (case sensitive)</param>
        /// <param name="typeName">The custom attribute's type name in metadata format (case sensitive)</param>
        /// <param name="ctor">Constructor of the custom attribute.</param>
        /// <param name="ignoreCase">Should case be ignored for name comparison?</param>
        /// <returns>true if match is found</returns>
        private static bool IsTargetAttribute(
            MetadataReader metadataReader,
            CustomAttributeHandle customAttribute,
            string namespaceName,
            string typeName,
            out EntityHandle ctor,
            bool ignoreCase)
        {
            Debug.Assert(namespaceName != null);
            Debug.Assert(typeName != null);

            EntityHandle ctorType;
            StringHandle ctorTypeNamespace;
            StringHandle ctorTypeName;

            if (!GetTypeAndConstructor(metadataReader, customAttribute, out ctorType, out ctor))
            {
                return false;
            }

            if (!GetAttributeNamespaceAndName(metadataReader, ctorType, out ctorTypeNamespace, out ctorTypeName))
            {
                return false;
            }

            try
            {
                return StringEquals(metadataReader, ctorTypeName, typeName, ignoreCase)
                    && StringEquals(metadataReader, ctorTypeNamespace, namespaceName, ignoreCase);
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns MetadataToken for assembly ref matching name
        /// </summary>
        /// <param name="assemblyName">The assembly name in metadata format (case sensitive)</param>
        /// <returns>Matching assembly ref token or nil (0)</returns>
        internal AssemblyReferenceHandle GetAssemblyRef(string assemblyName)
        {
            Debug.Assert(assemblyName != null);

            try
            {
                // Iterate over assembly ref rows
                foreach (var assemblyRef in MetadataReader.AssemblyReferences)
                {
                    // Check whether matching name                    
                    if (MetadataReader.StringComparer.Equals(MetadataReader.GetAssemblyReference(assemblyRef).Name, assemblyName))
                    {
                        // Return assembly ref token
                        return assemblyRef;
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            // Not found
            return default(AssemblyReferenceHandle);
        }

        internal AssemblyReference GetAssemblyRef(AssemblyReferenceHandle assemblyRef)
        {
            return MetadataReader.GetAssemblyReference(assemblyRef);
        }

        /// <summary>
        /// Returns MetadataToken for type ref matching resolution scope and name
        /// </summary>
        /// <param name="resolutionScope">The resolution scope token</param>
        /// <param name="namespaceName">The namespace name in metadata format (case sensitive)</param>
        /// <param name="typeName">The type name in metadata format (case sensitive)</param>
        /// <returns>Matching type ref token or nil (0)</returns>
        internal EntityHandle GetTypeRef(
            EntityHandle resolutionScope,
            string namespaceName,
            string typeName)
        {
            Debug.Assert(!resolutionScope.IsNil);
            Debug.Assert(namespaceName != null);
            Debug.Assert(typeName != null);

            try
            {
                // Iterate over type ref rows
                foreach (var handle in MetadataReader.TypeReferences)
                {
                    var typeRef = MetadataReader.GetTypeReference(handle);

                    // Check whether matching resolution scope
                    if (typeRef.ResolutionScope != resolutionScope)
                    {
                        continue;
                    }

                    // Check whether matching name
                    if (!MetadataReader.StringComparer.Equals(typeRef.Name, typeName))
                    {
                        continue;
                    }

                    if (MetadataReader.StringComparer.Equals(typeRef.Namespace, namespaceName))
                    {
                        // Return type ref token
                        return handle;
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            // Not found
            return default(TypeReferenceHandle);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public void GetTypeRefPropsOrThrow(
            TypeReferenceHandle handle,
            out string name,
            out string @namespace,
            out EntityHandle resolutionScope)
        {
            TypeReference typeRef = MetadataReader.GetTypeReference(handle);
            resolutionScope = typeRef.ResolutionScope;
            name = MetadataReader.GetString(typeRef.Name);
            Debug.Assert(MetadataHelpers.IsValidMetadataIdentifier(name));
            @namespace = MetadataReader.GetString(typeRef.Namespace);
        }

        /// <summary>
        /// Determine if custom attribute matches the target attribute.
        /// </summary>
        /// <param name="customAttribute">
        /// Handle of the custom attribute.
        /// </param>
        /// <param name="description">The attribute to match.</param>
        /// <returns>
        /// An index of the target constructor signature in
        /// signatures array, -1 if
        /// this is not the target attribute.
        /// </returns>
        internal int GetTargetAttributeSignatureIndex(CustomAttributeHandle customAttribute, AttributeDescription description)
        {
            return GetTargetAttributeSignatureIndex(MetadataReader, customAttribute, description, out _);
        }

        /// <summary>
        /// Determine if custom attribute matches the target attribute.
        /// </summary>
        /// <param name="metadataReader">
        /// The metadata reader.
        /// </param>
        /// <param name="customAttribute">
        /// Handle of the custom attribute.
        /// </param>
        /// <param name="description">The attribute to match.</param>
        /// <param name="matchedAttributeType">The custom attribute matched the target attribute namespace and type.</param>
        /// <returns>
        /// An index of the target constructor signature in
        /// signatures array, -1 if
        /// this is not the target attribute.
        /// </returns>
        private static int GetTargetAttributeSignatureIndex(MetadataReader metadataReader, CustomAttributeHandle customAttribute, AttributeDescription description, out bool matchedAttributeType)
        {
            const int No = -1;
            EntityHandle ctor;

            // Check namespace and type name and get signature if a match is found
            if (!IsTargetAttribute(metadataReader, customAttribute, description.Namespace, description.Name, out ctor, description.MatchIgnoringCase))
            {
                matchedAttributeType = false;
                return No;
            }

            matchedAttributeType = true;

            try
            {
                // Check signatures
                BlobReader sig = metadataReader.GetBlobReader(GetMethodSignatureOrThrow(metadataReader, ctor));

                for (int i = 0; i < description.Signatures.Length; i++)
                {
                    var targetSignature = description.Signatures[i];
                    Debug.Assert(targetSignature.Length >= 3);
                    sig.Reset();

                    // Make sure the headers match.
                    if (sig.RemainingBytes >= 3 &&
                        sig.ReadByte() == targetSignature[0] &&
                        sig.ReadByte() == targetSignature[1] &&
                        sig.ReadByte() == targetSignature[2])
                    {
                        int j = 3;
                        for (; j < targetSignature.Length; j++)
                        {
                            if (sig.RemainingBytes == 0)
                            {
                                // No more bytes in the signature
                                break;
                            }

                            SignatureTypeCode b = sig.ReadSignatureTypeCode();
                            if ((SignatureTypeCode)targetSignature[j] == b)
                            {
                                switch (b)
                                {
                                    case SignatureTypeCode.TypeHandle:
                                        EntityHandle token = sig.ReadTypeHandle();
                                        HandleKind tokenType = token.Kind;
                                        StringHandle name;
                                        StringHandle ns;

                                        if (tokenType == HandleKind.TypeDefinition)
                                        {
                                            TypeDefinitionHandle typeHandle = (TypeDefinitionHandle)token;

                                            if (IsNestedTypeDefOrThrow(metadataReader, typeHandle))
                                            {
                                                // At the moment, none of the well-known attributes take nested types.
                                                break; // Signature doesn't match.
                                            }

                                            TypeDefinition typeDef = metadataReader.GetTypeDefinition(typeHandle);
                                            name = typeDef.Name;
                                            ns = typeDef.Namespace;
                                        }
                                        else if (tokenType == HandleKind.TypeReference)
                                        {
                                            TypeReference typeRef = metadataReader.GetTypeReference((TypeReferenceHandle)token);

                                            if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
                                            {
                                                // At the moment, none of the well-known attributes take nested types.
                                                break; // Signature doesn't match.
                                            }

                                            name = typeRef.Name;
                                            ns = typeRef.Namespace;
                                        }
                                        else
                                        {
                                            break; // Signature doesn't match.
                                        }

                                        AttributeDescription.TypeHandleTargetInfo targetInfo = AttributeDescription.TypeHandleTargets[targetSignature[j + 1]];

                                        if (StringEquals(metadataReader, ns, targetInfo.Namespace, ignoreCase: false) &&
                                            StringEquals(metadataReader, name, targetInfo.Name, ignoreCase: false))
                                        {
                                            j++;
                                            continue;
                                        }

                                        break; // Signature doesn't match.

                                    case SignatureTypeCode.SZArray:
                                        // Verify array element type
                                        continue;

                                    default:
                                        continue;
                                }
                            }

                            break; // Signature doesn't match.
                        }

                        if (sig.RemainingBytes == 0 && j == targetSignature.Length)
                        {
                            // We found a match
                            return i;
                        }
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            return No;
        }

        /// <summary>
        /// Given a token for a constructor, return the token for the constructor's type and the blob containing the
        /// constructor's signature.
        /// </summary>
        /// <returns>True if the function successfully returns the type and signature.</returns>
        internal bool GetTypeAndConstructor(
            CustomAttributeHandle customAttribute,
            out EntityHandle ctorType,
            out EntityHandle attributeCtor)
        {
            return GetTypeAndConstructor(MetadataReader, customAttribute, out ctorType, out attributeCtor);
        }

        /// <summary>
        /// Given a token for a constructor, return the token for the constructor's type and the blob containing the
        /// constructor's signature.
        /// </summary>
        /// <returns>True if the function successfully returns the type and signature.</returns>
        private static bool GetTypeAndConstructor(
            MetadataReader metadataReader,
            CustomAttributeHandle customAttribute,
            out EntityHandle ctorType,
            out EntityHandle attributeCtor)
        {
            try
            {
                ctorType = default(EntityHandle);

                attributeCtor = metadataReader.GetCustomAttribute(customAttribute).Constructor;

                if (attributeCtor.Kind == HandleKind.MemberReference)
                {
                    MemberReference memberRef = metadataReader.GetMemberReference((MemberReferenceHandle)attributeCtor);

                    StringHandle ctorName = memberRef.Name;

                    if (!metadataReader.StringComparer.Equals(ctorName, WellKnownMemberNames.InstanceConstructorName))
                    {
                        // Not a constructor.
                        return false;
                    }

                    ctorType = memberRef.Parent;
                }
                else if (attributeCtor.Kind == HandleKind.MethodDefinition)
                {
                    var methodDef = metadataReader.GetMethodDefinition((MethodDefinitionHandle)attributeCtor);

                    if (!metadataReader.StringComparer.Equals(methodDef.Name, WellKnownMemberNames.InstanceConstructorName))
                    {
                        // Not a constructor.
                        return false;
                    }

                    ctorType = methodDef.GetDeclaringType();
                    Debug.Assert(!ctorType.IsNil);
                }
                else
                {
                    // invalid metadata
                    return false;
                }

                return true;
            }
            catch (BadImageFormatException)
            {
                ctorType = default(EntityHandle);
                attributeCtor = default(EntityHandle);
                return false;
            }
        }

        /// <summary>
        /// Given a token for a type, return the type's name and namespace.  Only works for top level types. 
        /// namespaceHandle will be NamespaceDefinitionHandle for defs and StringHandle for refs. 
        /// </summary>
        /// <returns>True if the function successfully returns the name and namespace.</returns>
        internal bool GetAttributeNamespaceAndName(EntityHandle typeDefOrRef, out StringHandle namespaceHandle, out StringHandle nameHandle)
        {
            return GetAttributeNamespaceAndName(MetadataReader, typeDefOrRef, out namespaceHandle, out nameHandle);
        }

        /// <summary>
        /// Given a token for a type, return the type's name and namespace.  Only works for top level types. 
        /// namespaceHandle will be NamespaceDefinitionHandle for defs and StringHandle for refs. 
        /// </summary>
        /// <returns>True if the function successfully returns the name and namespace.</returns>
        private static bool GetAttributeNamespaceAndName(MetadataReader metadataReader, EntityHandle typeDefOrRef, out StringHandle namespaceHandle, out StringHandle nameHandle)
        {
            nameHandle = default(StringHandle);
            namespaceHandle = default(StringHandle);

            try
            {
                if (typeDefOrRef.Kind == HandleKind.TypeReference)
                {
                    TypeReference typeRefRow = metadataReader.GetTypeReference((TypeReferenceHandle)typeDefOrRef);
                    HandleKind handleType = typeRefRow.ResolutionScope.Kind;

                    if (handleType == HandleKind.TypeReference || handleType == HandleKind.TypeDefinition)
                    {
                        // TODO - Support nested types.  
                        return false;
                    }

                    nameHandle = typeRefRow.Name;
                    namespaceHandle = typeRefRow.Namespace;
                }
                else if (typeDefOrRef.Kind == HandleKind.TypeDefinition)
                {
                    var def = metadataReader.GetTypeDefinition((TypeDefinitionHandle)typeDefOrRef);

                    if (IsNested(def.Attributes))
                    {
                        // TODO - Support nested types. 
                        return false;
                    }

                    nameHandle = def.Name;
                    namespaceHandle = def.Namespace;
                }
                else
                {
                    // unsupported metadata
                    return false;
                }

                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// For testing purposes only!!!
        /// </summary>
        internal void PretendThereArentNoPiaLocalTypes()
        {
            Debug.Assert(_lazyContainsNoPiaLocalTypes != ThreeState.True);
            _lazyContainsNoPiaLocalTypes = ThreeState.False;
        }

        internal bool ContainsNoPiaLocalTypes()
        {
            if (_lazyContainsNoPiaLocalTypes == ThreeState.Unknown)
            {
                try
                {
                    foreach (var attributeHandle in MetadataReader.CustomAttributes)
                    {
                        int signatureIndex = IsTypeIdentifierAttribute(attributeHandle);
                        if (signatureIndex != -1)
                        {
                            // We found a match
                            _lazyContainsNoPiaLocalTypes = ThreeState.True;

                            // We excluded attributes not applied on TypeDefs above:
                            var parent = (TypeDefinitionHandle)MetadataReader.GetCustomAttribute(attributeHandle).Parent;

                            RegisterNoPiaLocalType(parent, attributeHandle, signatureIndex);
                            return true;
                        }
                    }
                }
                catch (BadImageFormatException)
                { }

                _lazyContainsNoPiaLocalTypes = ThreeState.False;
            }

            return _lazyContainsNoPiaLocalTypes == ThreeState.True;
        }

        internal bool HasNullableContextAttribute(EntityHandle token, out byte value)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.NullableContextAttribute);
            Debug.Assert(!info.HasValue || info.SignatureIndex == 0);

            if (!info.HasValue)
            {
                value = 0;
                return false;
            }

            return TryExtractValueFromAttribute(info.Handle, out value, s_attributeByteValueExtractor);
        }

        internal bool HasNullableAttribute(EntityHandle token, out byte defaultTransform, out ImmutableArray<byte> nullableTransforms)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.NullableAttribute);
            Debug.Assert(!info.HasValue || info.SignatureIndex == 0 || info.SignatureIndex == 1);

            defaultTransform = 0;
            nullableTransforms = default(ImmutableArray<byte>);

            if (!info.HasValue)
            {
                return false;
            }

            if (info.SignatureIndex == 0)
            {
                return TryExtractValueFromAttribute(info.Handle, out defaultTransform, s_attributeByteValueExtractor);
            }

            return TryExtractByteArrayValueFromAttribute(info.Handle, out nullableTransforms);
        }

        internal bool TryGetOverloadResolutionPriorityValue(EntityHandle token, out int decodedPriority)
        {
            AttributeInfo info = FindTargetAttribute(token, AttributeDescription.OverloadResolutionPriorityAttribute);
            Debug.Assert(!info.HasValue || info.SignatureIndex == 0);

            if (!info.HasValue)
            {
                decodedPriority = 0;
                return false;
            }

            return TryExtractValueFromAttribute(info.Handle, out decodedPriority, s_attributeIntValueExtractor);
        }

        #endregion

        #region TypeSpec helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal BlobReader GetTypeSpecificationSignatureReaderOrThrow(TypeSpecificationHandle typeSpec)
        {
            // TODO: Check validity of the typeSpec handle.
            BlobHandle signature = MetadataReader.GetTypeSpecification(typeSpec).Signature;

            // TODO: error checking offset in range
            return MetadataReader.GetBlobReader(signature);
        }

        #endregion

        #region MethodSpec helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal void GetMethodSpecificationOrThrow(MethodSpecificationHandle handle, out EntityHandle method, out BlobHandle instantiation)
        {
            var methodSpec = MetadataReader.GetMethodSpecification(handle);
            method = methodSpec.Method;
            instantiation = methodSpec.Signature;
        }

        #endregion

        #region GenericParam helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal void GetGenericParamPropsOrThrow(
            GenericParameterHandle handle,
            out string name,
            out GenericParameterAttributes flags)
        {
            GenericParameter row = MetadataReader.GetGenericParameter(handle);
            name = MetadataReader.GetString(row.Name);
            flags = row.Attributes;
        }

        #endregion

        #region MethodDef helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal string GetMethodDefNameOrThrow(MethodDefinitionHandle methodDef)
        {
            return MetadataReader.GetString(MetadataReader.GetMethodDefinition(methodDef).Name);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal BlobHandle GetMethodSignatureOrThrow(MethodDefinitionHandle methodDef)
        {
            return GetMethodSignatureOrThrow(MetadataReader, methodDef);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private static BlobHandle GetMethodSignatureOrThrow(MetadataReader metadataReader, MethodDefinitionHandle methodDef)
        {
            return metadataReader.GetMethodDefinition(methodDef).Signature;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal BlobHandle GetMethodSignatureOrThrow(EntityHandle methodDefOrRef)
        {
            return GetMethodSignatureOrThrow(MetadataReader, methodDefOrRef);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private static BlobHandle GetMethodSignatureOrThrow(MetadataReader metadataReader, EntityHandle methodDefOrRef)
        {
            switch (methodDefOrRef.Kind)
            {
                case HandleKind.MethodDefinition:
                    return GetMethodSignatureOrThrow(metadataReader, (MethodDefinitionHandle)methodDefOrRef);

                case HandleKind.MemberReference:
                    return GetSignatureOrThrow(metadataReader, (MemberReferenceHandle)methodDefOrRef);

                default:
                    throw ExceptionUtilities.UnexpectedValue(methodDefOrRef.Kind);
            }
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public MethodAttributes GetMethodDefFlagsOrThrow(MethodDefinitionHandle methodDef)
        {
            return MetadataReader.GetMethodDefinition(methodDef).Attributes;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal TypeDefinitionHandle FindContainingTypeOrThrow(MethodDefinitionHandle methodDef)
        {
            return MetadataReader.GetMethodDefinition(methodDef).GetDeclaringType();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal TypeDefinitionHandle FindContainingTypeOrThrow(FieldDefinitionHandle fieldDef)
        {
            return MetadataReader.GetFieldDefinition(fieldDef).GetDeclaringType();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal EntityHandle GetContainingTypeOrThrow(MemberReferenceHandle memberRef)
        {
            return MetadataReader.GetMemberReference(memberRef).Parent;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public void GetMethodDefPropsOrThrow(
            MethodDefinitionHandle methodDef,
            out string name,
            out MethodImplAttributes implFlags,
            out MethodAttributes flags,
            out int rva)
        {
            MethodDefinition methodRow = MetadataReader.GetMethodDefinition(methodDef);
            name = MetadataReader.GetString(methodRow.Name);
            implFlags = methodRow.ImplAttributes;
            flags = methodRow.Attributes;
            rva = methodRow.RelativeVirtualAddress;
            Debug.Assert(rva >= 0);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal void GetMethodImplPropsOrThrow(
            MethodImplementationHandle methodImpl,
            out EntityHandle body,
            out EntityHandle declaration)
        {
            var impl = MetadataReader.GetMethodImplementation(methodImpl);
            body = impl.MethodBody;
            declaration = impl.MethodDeclaration;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal GenericParameterHandleCollection GetGenericParametersForMethodOrThrow(MethodDefinitionHandle methodDef)
        {
            return MetadataReader.GetMethodDefinition(methodDef).GetGenericParameters();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal ParameterHandleCollection GetParametersOfMethodOrThrow(MethodDefinitionHandle methodDef)
        {
            return MetadataReader.GetMethodDefinition(methodDef).GetParameters();
        }

        internal DllImportData GetDllImportData(MethodDefinitionHandle methodDef)
        {
            try
            {
                var methodImport = MetadataReader.GetMethodDefinition(methodDef).GetImport();
                if (methodImport.Module.IsNil)
                {
                    // TODO (tomat): report an error?
                    return null;
                }

                string moduleName = GetModuleRefNameOrThrow(methodImport.Module);
                string entryPointName = MetadataReader.GetString(methodImport.Name);
                MethodImportAttributes flags = (MethodImportAttributes)methodImport.Attributes;

                return new DllImportData(moduleName, entryPointName, flags);
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        #endregion

        #region MemberRef helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public string GetMemberRefNameOrThrow(MemberReferenceHandle memberRef)
        {
            return GetMemberRefNameOrThrow(MetadataReader, memberRef);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private static string GetMemberRefNameOrThrow(MetadataReader metadataReader, MemberReferenceHandle memberRef)
        {
            return metadataReader.GetString(metadataReader.GetMemberReference(memberRef).Name);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal BlobHandle GetSignatureOrThrow(MemberReferenceHandle memberRef)
        {
            return GetSignatureOrThrow(MetadataReader, memberRef);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private static BlobHandle GetSignatureOrThrow(MetadataReader metadataReader, MemberReferenceHandle memberRef)
        {
            return metadataReader.GetMemberReference(memberRef).Signature;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public void GetMemberRefPropsOrThrow(
            MemberReferenceHandle memberRef,
            out EntityHandle @class,
            out string name,
            out byte[] signature)
        {
            MemberReference row = MetadataReader.GetMemberReference(memberRef);
            @class = row.Parent;
            name = MetadataReader.GetString(row.Name);
            signature = MetadataReader.GetBlobBytes(row.Signature);
        }

        #endregion MemberRef helpers

        #region ParamDef helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal void GetParamPropsOrThrow(
            ParameterHandle parameterDef,
            out string name,
            out ParameterAttributes flags)
        {
            Parameter parameter = MetadataReader.GetParameter(parameterDef);
            name = MetadataReader.GetString(parameter.Name);
            flags = parameter.Attributes;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal string GetParamNameOrThrow(ParameterHandle parameterDef)
        {
            Parameter parameter = MetadataReader.GetParameter(parameterDef);
            return MetadataReader.GetString(parameter.Name);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal int GetParameterSequenceNumberOrThrow(ParameterHandle param)
        {
            return MetadataReader.GetParameter(param).SequenceNumber;
        }

        #endregion

        #region PropertyDef helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal string GetPropertyDefNameOrThrow(PropertyDefinitionHandle propertyDef)
        {
            return MetadataReader.GetString(MetadataReader.GetPropertyDefinition(propertyDef).Name);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal BlobHandle GetPropertySignatureOrThrow(PropertyDefinitionHandle propertyDef)
        {
            return MetadataReader.GetPropertyDefinition(propertyDef).Signature;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal void GetPropertyDefPropsOrThrow(
            PropertyDefinitionHandle propertyDef,
            out string name,
            out PropertyAttributes flags)
        {
            PropertyDefinition property = MetadataReader.GetPropertyDefinition(propertyDef);
            name = MetadataReader.GetString(property.Name);
            flags = property.Attributes;
        }

        #endregion

        #region EventDef helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal string GetEventDefNameOrThrow(EventDefinitionHandle eventDef)
        {
            return MetadataReader.GetString(MetadataReader.GetEventDefinition(eventDef).Name);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal void GetEventDefPropsOrThrow(
            EventDefinitionHandle eventDef,
            out string name,
            out EventAttributes flags,
            out EntityHandle type)
        {
            EventDefinition eventRow = MetadataReader.GetEventDefinition(eventDef);
            name = MetadataReader.GetString(eventRow.Name);
            flags = eventRow.Attributes;
            type = eventRow.Type;
        }

        #endregion

        #region FieldDef helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public string GetFieldDefNameOrThrow(FieldDefinitionHandle fieldDef)
        {
            return MetadataReader.GetString(MetadataReader.GetFieldDefinition(fieldDef).Name);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal BlobHandle GetFieldSignatureOrThrow(FieldDefinitionHandle fieldDef)
        {
            return MetadataReader.GetFieldDefinition(fieldDef).Signature;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public FieldAttributes GetFieldDefFlagsOrThrow(FieldDefinitionHandle fieldDef)
        {
            return MetadataReader.GetFieldDefinition(fieldDef).Attributes;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public void GetFieldDefPropsOrThrow(
            FieldDefinitionHandle fieldDef,
            out string name,
            out FieldAttributes flags)
        {
            FieldDefinition fieldRow = MetadataReader.GetFieldDefinition(fieldDef);

            name = MetadataReader.GetString(fieldRow.Name);
            flags = fieldRow.Attributes;
        }

        internal ConstantValue GetParamDefaultValue(ParameterHandle param)
        {
            Debug.Assert(!param.IsNil);

            try
            {
                var constantHandle = MetadataReader.GetParameter(param).GetDefaultValue();

                // TODO: Error checking: Throw an error if the table entry cannot be found
                return constantHandle.IsNil ? ConstantValue.Bad : GetConstantValueOrThrow(constantHandle);
            }
            catch (BadImageFormatException)
            {
                return ConstantValue.Bad;
            }
        }

        internal ConstantValue GetConstantFieldValue(FieldDefinitionHandle fieldDef)
        {
            Debug.Assert(!fieldDef.IsNil);

            try
            {
                var constantHandle = MetadataReader.GetFieldDefinition(fieldDef).GetDefaultValue();

                // TODO: Error checking: Throw an error if the table entry cannot be found
                return constantHandle.IsNil ? ConstantValue.Bad : GetConstantValueOrThrow(constantHandle);
            }
            catch (BadImageFormatException)
            {
                return ConstantValue.Bad;
            }
        }

        #endregion

        #region Attribute Helpers

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public CustomAttributeHandleCollection GetCustomAttributesOrThrow(EntityHandle handle)
        {
            return MetadataReader.GetCustomAttributes(handle);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public BlobHandle GetCustomAttributeValueOrThrow(CustomAttributeHandle handle)
        {
            return MetadataReader.GetCustomAttribute(handle).Value;
        }

        #endregion

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private BlobHandle GetMarshallingDescriptorHandleOrThrow(EntityHandle fieldOrParameterToken)
        {
            return fieldOrParameterToken.Kind == HandleKind.FieldDefinition ?
                MetadataReader.GetFieldDefinition((FieldDefinitionHandle)fieldOrParameterToken).GetMarshallingDescriptor() :
                MetadataReader.GetParameter((ParameterHandle)fieldOrParameterToken).GetMarshallingDescriptor();
        }

        internal UnmanagedType GetMarshallingType(EntityHandle fieldOrParameterToken)
        {
            try
            {
                var blob = GetMarshallingDescriptorHandleOrThrow(fieldOrParameterToken);

                if (blob.IsNil)
                {
                    // TODO (tomat): report error:
                    return 0;
                }

                byte firstByte = MetadataReader.GetBlobReader(blob).ReadByte();

                // return only valid types, other values are not interesting for the compiler:
                return firstByte <= 0x50 ? (UnmanagedType)firstByte : 0;
            }
            catch (BadImageFormatException)
            {
                return 0;
            }
        }

        internal ImmutableArray<byte> GetMarshallingDescriptor(EntityHandle fieldOrParameterToken)
        {
            try
            {
                var blob = GetMarshallingDescriptorHandleOrThrow(fieldOrParameterToken);
                if (blob.IsNil)
                {
                    // TODO (tomat): report error:
                    return ImmutableArray<byte>.Empty;
                }

                return MetadataReader.GetBlobBytes(blob).AsImmutableOrNull();
            }
            catch (BadImageFormatException)
            {
                return ImmutableArray<byte>.Empty;
            }
        }

        internal int? GetFieldOffset(FieldDefinitionHandle fieldDef)
        {
            try
            {
                int offset = MetadataReader.GetFieldDefinition(fieldDef).GetOffset();
                if (offset == -1)
                {
                    return null;
                }

                return offset;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private ConstantValue GetConstantValueOrThrow(ConstantHandle handle)
        {
            var constantRow = MetadataReader.GetConstant(handle);
            BlobReader reader = MetadataReader.GetBlobReader(constantRow.Value);
            switch (constantRow.TypeCode)
            {
                case ConstantTypeCode.Boolean:
                    return ConstantValue.Create(reader.ReadBoolean());

                case ConstantTypeCode.Char:
                    return ConstantValue.Create(reader.ReadChar());

                case ConstantTypeCode.SByte:
                    return ConstantValue.Create(reader.ReadSByte());

                case ConstantTypeCode.Int16:
                    return ConstantValue.Create(reader.ReadInt16());

                case ConstantTypeCode.Int32:
                    return ConstantValue.Create(reader.ReadInt32());

                case ConstantTypeCode.Int64:
                    return ConstantValue.Create(reader.ReadInt64());

                case ConstantTypeCode.Byte:
                    return ConstantValue.Create(reader.ReadByte());

                case ConstantTypeCode.UInt16:
                    return ConstantValue.Create(reader.ReadUInt16());

                case ConstantTypeCode.UInt32:
                    return ConstantValue.Create(reader.ReadUInt32());

                case ConstantTypeCode.UInt64:
                    return ConstantValue.Create(reader.ReadUInt64());

                case ConstantTypeCode.Single:
                    return ConstantValue.Create(reader.ReadSingle());

                case ConstantTypeCode.Double:
                    return ConstantValue.Create(reader.ReadDouble());

                case ConstantTypeCode.String:
                    return ConstantValue.Create(reader.ReadUTF16(reader.Length));

                case ConstantTypeCode.NullReference:
                    // Partition II section 22.9:
                    // The encoding of Type for the nullref value is ELEMENT_TYPE_CLASS with a Value of a 4-byte zero.
                    // Unlike uses of ELEMENT_TYPE_CLASS in signatures, this one is not followed by a type token.
                    if (reader.ReadUInt32() == 0)
                    {
                        return ConstantValue.Null;
                    }

                    break;
            }

            return ConstantValue.Bad;
        }

        internal (int FirstIndex, int SecondIndex) GetAssemblyRefsForForwardedType(string fullName, bool ignoreCase, out string matchedName)
        {
            EnsureForwardTypeToAssemblyMap();

            if (ignoreCase)
            {
                // We should only use this functionality when computing diagnostics, so we lazily construct
                // a case-insensitive map when necessary. Note that we can't store the original map
                // case-insensitively, since real metadata name lookup has to remain case sensitive.
                ensureCaseInsensitiveDictionary();

                if (_lazyCaseInsensitiveForwardedTypesToAssemblyIndexMap.TryGetValue(fullName, out var value))
                {
                    matchedName = value.OriginalName;
                    return (value.FirstIndex, value.SecondIndex);
                }
            }
            else
            {
                if (_lazyForwardedTypesToAssemblyIndexMap.TryGetValue(fullName, out (int FirstIndex, int SecondIndex) assemblyIndices))
                {
                    matchedName = fullName;
                    return assemblyIndices;
                }
            }

            matchedName = null;
            return (FirstIndex: -1, SecondIndex: -1);

            void ensureCaseInsensitiveDictionary()
            {
                if (_lazyCaseInsensitiveForwardedTypesToAssemblyIndexMap != null)
                {
                    return;
                }

                if (_lazyForwardedTypesToAssemblyIndexMap.Count == 0)
                {
                    _lazyCaseInsensitiveForwardedTypesToAssemblyIndexMap = s_sharedEmptyCaseInsensitiveForwardedTypes;
                    return;
                }

                var caseInsensitiveMap = new Dictionary<string, (string OriginalName, int FirstIndex, int SecondIndex)>(StringComparer.OrdinalIgnoreCase);

                foreach (var (key, (firstIndex, secondIndex)) in _lazyForwardedTypesToAssemblyIndexMap)
                {
                    _ = caseInsensitiveMap.TryAdd(key, (key, firstIndex, secondIndex));
                }

                _lazyCaseInsensitiveForwardedTypesToAssemblyIndexMap = caseInsensitiveMap;
            }
        }

        internal IEnumerable<KeyValuePair<string, (int FirstIndex, int SecondIndex)>> GetForwardedTypes()
        {
            EnsureForwardTypeToAssemblyMap();
            return _lazyForwardedTypesToAssemblyIndexMap;
        }

#nullable enable
        [MemberNotNull(nameof(_lazyForwardedTypesToAssemblyIndexMap))]
        private void EnsureForwardTypeToAssemblyMap()
        {
            if (_lazyForwardedTypesToAssemblyIndexMap == null)
            {
                Dictionary<string, (int FirstIndex, int SecondIndex)>? typesToAssemblyIndexMap = null;

                try
                {
                    var forwarders = MetadataReader.ExportedTypes;
                    foreach (var handle in forwarders)
                    {
                        ExportedType exportedType = MetadataReader.GetExportedType(handle);
                        if (!exportedType.IsForwarder)
                        {
                            continue;
                        }

                        AssemblyReferenceHandle refHandle = (AssemblyReferenceHandle)exportedType.Implementation;
                        if (refHandle.IsNil)
                        {
                            continue;
                        }

                        int referencedAssemblyIndex;
                        try
                        {
                            referencedAssemblyIndex = this.GetAssemblyReferenceIndexOrThrow(refHandle);
                        }
                        catch (BadImageFormatException)
                        {
                            continue;
                        }

                        if (referencedAssemblyIndex < 0 || referencedAssemblyIndex >= this.ReferencedAssemblies.Length)
                        {
                            continue;
                        }

                        string name = MetadataReader.GetString(exportedType.Name);
                        StringHandle ns = exportedType.Namespace;
                        if (!ns.IsNil)
                        {
                            string namespaceString = MetadataReader.GetString(ns);
                            if (namespaceString.Length > 0)
                            {
                                name = namespaceString + "." + name;
                            }
                        }

                        typesToAssemblyIndexMap ??= new Dictionary<string, (int FirstIndex, int SecondIndex)>();

                        (int FirstIndex, int SecondIndex) indices;

                        if (typesToAssemblyIndexMap.TryGetValue(name, out indices))
                        {
                            Debug.Assert(indices.FirstIndex >= 0, "Not allowed to store a negative (non-existent) index in typesToAssemblyIndexMap");

                            // Store it only if it was not a duplicate
                            if (indices.FirstIndex != referencedAssemblyIndex && indices.SecondIndex < 0)
                            {
                                indices.SecondIndex = referencedAssemblyIndex;
                                typesToAssemblyIndexMap[name] = indices;
                            }
                        }
                        else
                        {
                            typesToAssemblyIndexMap.Add(name, (FirstIndex: referencedAssemblyIndex, SecondIndex: -1));
                        }
                    }
                }
                catch (BadImageFormatException)
                { }

                if (typesToAssemblyIndexMap == null)
                {
                    _lazyForwardedTypesToAssemblyIndexMap = s_sharedEmptyForwardedTypes;
                }
                else
                {
                    _lazyForwardedTypesToAssemblyIndexMap = typesToAssemblyIndexMap;
                }
            }
        }
#nullable disable

        internal IdentifierCollection TypeNames
        {
            get
            {
                return _lazyTypeNameCollection.Value;
            }
        }

        internal IdentifierCollection NamespaceNames
        {
            get
            {
                return _lazyNamespaceNameCollection.Value;
            }
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal PropertyAccessors GetPropertyMethodsOrThrow(PropertyDefinitionHandle propertyDef)
        {
            return MetadataReader.GetPropertyDefinition(propertyDef).GetAccessors();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal EventAccessors GetEventMethodsOrThrow(EventDefinitionHandle eventDef)
        {
            return MetadataReader.GetEventDefinition(eventDef).GetAccessors();
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal int GetAssemblyReferenceIndexOrThrow(AssemblyReferenceHandle assemblyRef)
        {
            return MetadataReader.GetRowNumber(assemblyRef) - 1;
        }

        internal static bool IsNested(TypeAttributes flags)
        {
            return (flags & ((TypeAttributes)0x00000006)) != 0;
        }

        /// <summary>
        /// Returns true if method IL can be retrieved from the module.
        /// </summary>
        internal bool HasIL
        {
            get { return IsEntireImageAvailable; }
        }

        /// <summary>
        /// Returns true if the full image of the module is available.
        /// </summary>
        internal bool IsEntireImageAvailable
        {
            get { return _peReaderOpt != null && _peReaderOpt.IsEntireImageAvailable; }
        }

        /// <exception cref="BadImageFormatException">Invalid metadata.</exception>
        internal MethodBodyBlock GetMethodBodyOrThrow(MethodDefinitionHandle methodHandle)
        {
            // we shouldn't ask for method IL if we don't have PE image
            Debug.Assert(_peReaderOpt != null);

            MethodDefinition method = MetadataReader.GetMethodDefinition(methodHandle);
            if ((method.ImplAttributes & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL ||
                 method.RelativeVirtualAddress == 0)
            {
                return null;
            }

            return _peReaderOpt.GetMethodBody(method.RelativeVirtualAddress);
        }

        // TODO: remove, API should be provided by MetadataReader
        private static bool StringEquals(MetadataReader metadataReader, StringHandle nameHandle, string name, bool ignoreCase)
        {
            if (ignoreCase)
            {
                return string.Equals(metadataReader.GetString(nameHandle), name, StringComparison.OrdinalIgnoreCase);
            }

            return metadataReader.StringComparer.Equals(nameHandle, name);
        }

        // Provides a UTF-8 decoder to the MetadataReader that reuses strings from the string table
        // rather than allocating on each call to MetadataReader.GetString(handle).
        private sealed class StringTableDecoder : MetadataStringDecoder
        {
            public static readonly StringTableDecoder Instance = new StringTableDecoder();

            private StringTableDecoder() : base(System.Text.Encoding.UTF8) { }

            public override unsafe string GetString(byte* bytes, int byteCount)
            {
                return StringTable.AddSharedUtf8(new ReadOnlySpan<byte>(bytes, byteCount));
            }
        }

        public ModuleMetadata GetNonDisposableMetadata() => _owner.Copy();
    }
}
