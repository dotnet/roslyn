// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal abstract class MetadataWriter
    {
        private static readonly Encoding Utf8Encoding = Encoding.UTF8;
        
        /// <summary>
        /// This is the maximum length of a type or member name in metadata, assuming
        /// the name is in UTF-8 format and not (yet) null-terminated.
        /// </summary>
        /// <remarks>
        /// Source names may have to be shorter still to accommodate mangling.
        /// Used for event names, field names, property names, field names, method def names,
        /// member ref names, type def (full) names, type ref (full) names, exported type
        /// (full) names, parameter names, manifest resource names, and unmanaged method names
        /// (ImplMap table).
        /// 
        /// See CLI Part II, section 22.
        /// </remarks>
        internal const int NameLengthLimit = 1024 - 1; //MAX_CLASS_NAME = 1024 in dev11

        /// <summary>
        /// This is the maximum length of a path in metadata, assuming the path is in UTF-8
        /// format and not (yet) null-terminated.
        /// </summary>
        /// <remarks>
        /// Used for file names, module names, and module ref names.
        /// 
        /// See CLI Part II, section 22.
        /// </remarks>
        internal const int PathLengthLimit = 260 - 1; //MAX_PATH = 1024 in dev11

        /// <summary>
        /// This is the maximum length of a string in the PDB, assuming it is in UTF-8 format 
        /// and not (yet) null-terminated.
        /// </summary>
        /// <remarks>
        /// Used for import strings, locals, and local constants.
        /// </remarks>
        internal const int PdbLengthLimit = 2046; // Empirical, based on when ISymUnmanagedWriter2 methods start throwing.

        private readonly int numTypeDefsEstimate;
        private readonly bool deterministic;

        // If true, it is allowed to have methods not have bodies (for emitting metadata-only assembly)
        internal readonly bool allowMissingMethodBodies;

        // A map of method body to RVA. Used for deduplication of small bodies.
        private readonly Dictionary<byte[], uint> smallMethodBodies;

        protected MetadataWriter(
            EmitContext context,
            CommonMessageProvider messageProvider,
            bool allowMissingMethodBodies,
            bool deterministic,
            CancellationToken cancellationToken)
        {
            this.module = context.Module;
            this.deterministic = deterministic;
            this.allowMissingMethodBodies = allowMissingMethodBodies;

            // EDMAURER provide some reasonable size estimates for these that will avoid
            // much of the reallocation that would occur when growing these from empty.
            signatureIndex = new Dictionary<ISignature, uint>(module.HintNumberOfMethodDefinitions); //ignores field signatures

            numTypeDefsEstimate = module.HintNumberOfMethodDefinitions / 6;
            exportedTypeIndex = new Dictionary<ITypeReference, uint>(numTypeDefsEstimate);
            exportedTypeList = new List<ITypeReference>(numTypeDefsEstimate);

            this.Context = context;
            this.messageProvider = messageProvider;
            this.cancellationToken = cancellationToken;

            // Add zero-th entry to heaps. 
            // Delta metadata requires these to avoid nil generation-relative handles, 
            // which are technically viable but confusing.
            this.blobWriter.WriteByte(0);
            this.stringWriter.WriteByte(0);
            this.userStringWriter.WriteByte(0);

            this.smallMethodBodies = new Dictionary<byte[], uint>(ByteSequenceComparer.Instance);
        }

        private int NumberOfTypeDefsEstimate { get { return numTypeDefsEstimate; } }

        /// <summary>
        /// Returns true if writing full metadata, false if writing delta.
        /// </summary>
        internal bool IsFullMetadata
        {
            get { return this.Generation == 0; }
        }

        /// <summary>
        /// True if writing delta metadata in a minimal format.
        /// </summary>
        private bool IsMinimalDelta
        {
            get { return !IsFullMetadata; }
        }

        internal Guid ModuleVersionId
        {
            get { return this.module.PersistentIdentifier; }
        }

        /// <summary>
        /// Returns metadata generation ordinal. Zero for
        /// full metadata and non-zero for delta.
        /// </summary>
        protected abstract ushort Generation { get; }

        /// <summary>
        /// Returns unique Guid for this delta, or default(Guid)
        /// if full metadata.
        /// </summary>
        protected abstract Guid EncId { get; }

        /// <summary>
        /// Returns Guid of previous delta, or default(Guid)
        /// if full metadata or generation 1 delta.
        /// </summary>
        protected abstract Guid EncBaseId { get; }

        /// <summary>
        /// Returns true if the metadata stream should be compressed.
        /// </summary>
        protected abstract bool CompressMetadataStream { get; }

        /// <summary>
        /// Returns true and the 1-based index of the type definition
        /// if the type definition is recognized. Otherwise returns false.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract bool TryGetTypeDefIndex(ITypeDefinition def, out uint index);

        /// <summary>
        /// The 1-based index of the type definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract uint GetTypeDefIndex(ITypeDefinition def);

        /// <summary>
        /// The type definition at the 0-based index into the full set. Deltas
        /// are only required to support indexing into current generation.
        /// </summary>
        protected abstract ITypeDefinition GetTypeDef(int index);

        /// <summary>
        /// The type definitions to be emitted, in row order. These
        /// are just the type definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeDefinition> GetTypeDefs();

        /// <summary>
        /// The 1-based index of the event definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract uint GetEventDefIndex(IEventDefinition def);

        /// <summary>
        /// The event definitions to be emitted, in row order. These
        /// are just the event definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IEventDefinition> GetEventDefs();

        /// <summary>
        /// The 1-based index of the field definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract uint GetFieldDefIndex(IFieldDefinition def);

        /// <summary>
        /// The field definitions to be emitted, in row order. These
        /// are just the field definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IFieldDefinition> GetFieldDefs();

        /// <summary>
        /// Returns true and the 1-based index of the method definition
        /// if the method definition is recognized. Otherwise returns false.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract bool TryGetMethodDefIndex(IMethodDefinition def, out uint index);

        /// <summary>
        /// The 1-based index of the method definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract uint GetMethodDefIndex(IMethodDefinition def);

        /// <summary>
        /// The method definition at the 0-based index into the full set. Deltas
        /// are only required to support indexing into current generation.
        /// </summary>
        protected abstract IMethodDefinition GetMethodDef(int index);

        /// <summary>
        /// The method definitions to be emitted, in row order. These
        /// are just the method definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IMethodDefinition> GetMethodDefs();

        /// <summary>
        /// The 1-based index of the property definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract uint GetPropertyDefIndex(IPropertyDefinition def);

        /// <summary>
        /// The property definitions to be emitted, in row order. These
        /// are just the property definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IPropertyDefinition> GetPropertyDefs();

        /// <summary>
        /// The 1-based index of the parameter definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract uint GetParameterDefIndex(IParameterDefinition def);

        /// <summary>
        /// The parameter definitions to be emitted, in row order. These
        /// are just the parameter definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IParameterDefinition> GetParameterDefs();

        /// <summary>
        /// The generic parameter definitions to be emitted, in row order. These
        /// are just the generic parameter definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IGenericParameter> GetGenericParameters();

        /// <summary>
        /// The 1-based index of the first field of the type.
        /// </summary>
        protected abstract uint GetFieldDefIndex(INamedTypeDefinition typeDef);

        /// <summary>
        /// The 1-based index of the first method of the type.
        /// </summary>
        protected abstract uint GetMethodDefIndex(INamedTypeDefinition typeDef);

        /// <summary>
        /// The 1-based index of the first parameter of the method.
        /// </summary>
        protected abstract uint GetParameterDefIndex(IMethodDefinition methodDef);

        /// <summary>
        /// Return the 1-based index of the assembly reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract uint GetOrAddAssemblyRefIndex(IAssemblyReference reference);

        /// <summary>
        /// The assembly references to be emitted, in row order. These
        /// are just the assembly references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IAssemblyReference> GetAssemblyRefs();

        // ModuleRef table contains module names for TypeRefs that target types in netmodules (represented by IModuleReference),
        // and module names specified by P/Invokes (plain strings). Names in the table must be unique and are case sensitive.
        //
        // Spec 22.31 (ModuleRef : 0x1A)
        // "Name should match an entry in the Name column of the File table. Moreover, that entry shall enable the 
        // CLI to locate the target module (typically it might name the file used to hold the module)"
        // 
        // This is not how the Dev10 compilers and ILASM work. An entry is added to File table only for resources and netmodules.
        // Entries aren't added for P/Invoked modules.

        /// <summary>
        /// Return the 1-based index of the module reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract uint GetOrAddModuleRefIndex(string reference);

        /// <summary>
        /// The module references to be emitted, in row order. These
        /// are just the module references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<string> GetModuleRefs();

        /// <summary>
        /// Return the 1-based index of the member reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract uint GetOrAddMemberRefIndex(ITypeMemberReference reference);

        /// <summary>
        /// The member references to be emitted, in row order. These
        /// are just the member references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeMemberReference> GetMemberRefs();

        /// <summary>
        /// Return the 1-based index of the method spec, adding
        /// the spec to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract uint GetOrAddMethodSpecIndex(IGenericMethodInstanceReference reference);

        /// <summary>
        /// The method specs to be emitted, in row order. These
        /// are just the method specs from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IGenericMethodInstanceReference> GetMethodSpecs();

        /// <summary>
        /// Return true and the 1-based index of the type reference
        /// if the reference is available in the current generation.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract bool TryGetTypeRefIndex(ITypeReference reference, out uint index);

        /// <summary>
        /// Return the 1-based index of the type reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract uint GetOrAddTypeRefIndex(ITypeReference reference);

        /// <summary>
        /// The type references to be emitted, in row order. These
        /// are just the type references from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeReference> GetTypeRefs();

        /// <summary>
        /// Return the 1-based index of the type spec, adding
        /// the spec to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract uint GetOrAddTypeSpecIndex(ITypeReference reference);

        /// <summary>
        /// The type specs to be emitted, in row order. These
        /// are just the type specs from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<ITypeReference> GetTypeSpecs();

        /// <summary>
        /// Return the 1-based index of the signature index, adding
        /// the signature to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract uint GetOrAddStandAloneSignatureIndex(uint blobIndex);

        /// <summary>
        /// The signature indices to be emitted, in row order. These
        /// are just the signature indices from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<uint> GetStandAloneSignatures();

        protected abstract IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes(IModule module);

        protected abstract void CreateIndicesForNonTypeMembers(ITypeDefinition typeDef);

        /// <summary>
        /// Offset into full metadata blob stream.
        /// </summary>
        protected abstract uint GetBlobStreamOffset();

        /// <summary>
        /// Offset into full metadata string stream.
        /// </summary>
        protected abstract uint GetStringStreamOffset();

        /// <summary>
        /// Offset into full metadata user string stream.
        /// </summary>
        protected abstract uint GetUserStringStreamOffset();

        /// <summary>
        /// Return a visitor for traversing all references to be emitted.
        /// </summary>
        protected abstract ReferenceIndexer CreateReferenceVisitor();

        /// <summary>
        /// Invoked after serializing metadata tables.
        /// </summary>
        protected virtual void OnBeforeHeapsAligned()
        {
        }

        /// <summary>
        /// Populate EventMap table.
        /// </summary>
        protected abstract void PopulateEventMapTableRows(List<EventMapRow> table);

        /// <summary>
        /// Populate PropertyMap table.
        /// </summary>
        protected abstract void PopulatePropertyMapTableRows(List<PropertyMapRow> table);

        /// <summary>
        /// Populate EncLog table.
        /// </summary>
        protected abstract void PopulateEncLogTableRows(List<EncLogRow> table, ImmutableArray<int> rowCounts);

        /// <summary>
        /// Populate EncMap table.
        /// </summary>
        protected abstract void PopulateEncMapTableRows(List<EncMapRow> table, ImmutableArray<int> rowCounts);

        protected abstract void ReportReferencesToAddedSymbols();

        // If true, it is allowed to have methods not have bodies (for emitting metadata-only
        // assembly)
        private readonly CancellationToken cancellationToken;
        protected readonly IModule module;
        public readonly EmitContext Context;
        protected readonly CommonMessageProvider messageProvider;

        // progress:
        private bool streamsAreComplete;
        private bool tableIndicesAreComplete;

        private uint[] pseudoSymbolTokenToTokenMap;
        private IReference[] pseudoSymbolTokenToReferenceMap;
        private uint[] pseudoStringTokenToTokenMap;
        private List<string> pseudoStringTokenToStringMap;
        private ReferenceIndexer referenceVisitor;

        // #String heap
        private Dictionary<string, uint> stringIndex = new Dictionary<string, uint>(128);
        private Dictionary<uint, uint> stringIndexMap;
        protected readonly BinaryWriter stringWriter = new BinaryWriter(new MemoryStream(1024));

        // #US heap
        private readonly Dictionary<string, uint> userStringIndex = new Dictionary<string, uint>();
        protected readonly BinaryWriter userStringWriter = new BinaryWriter(new MemoryStream(1024), true);

        // #Blob heap
        private readonly Dictionary<ImmutableArray<byte>, uint> blobIndex = new Dictionary<ImmutableArray<byte>, uint>(ByteSequenceComparer.Instance);
        protected readonly BinaryWriter blobWriter = new BinaryWriter(new MemoryStream(1024));

        // #GUID heap
        private readonly Dictionary<Guid, uint> guidIndex = new Dictionary<Guid, uint>();
        protected readonly BinaryWriter guidWriter = new BinaryWriter(new MemoryStream(16)); // full metadata has just a single guid

        private readonly Dictionary<ICustomAttribute, uint> customAtributeSignatureIndex = new Dictionary<ICustomAttribute, uint>();
        private readonly Dictionary<ITypeReference, uint> typeSpecSignatureIndex = new Dictionary<ITypeReference, uint>();
        private readonly Dictionary<ITypeReference, uint> exportedTypeIndex;
        private readonly List<ITypeReference> exportedTypeList;
        private readonly Dictionary<string, uint> fileRefIndex = new Dictionary<string, uint>(32);  //more than enough in most cases
        private readonly List<IFileReference> fileRefList = new List<IFileReference>(32);
        private readonly Dictionary<IFieldReference, uint> fieldSignatureIndex = new Dictionary<IFieldReference, uint>();
        private readonly Dictionary<ISignature, uint> signatureIndex;
        private readonly Dictionary<IMarshallingInformation, uint> marshallingDescriptorIndex = new Dictionary<IMarshallingInformation, uint>();
        protected readonly List<MethodImplementation> methodImplList = new List<MethodImplementation>();
        private readonly Dictionary<IGenericMethodInstanceReference, uint> methodInstanceSignatureIndex = new Dictionary<IGenericMethodInstanceReference, uint>();
        
        // Well known dummy cor library types whose refs are used for attaching assembly attributes off within net modules
        // There is no guarantee the types actually exist in a cor library
        internal static readonly string dummyAssemblyAttributeParentNamespace = "System.Runtime.CompilerServices";
        internal static readonly string dummyAssemblyAttributeParentName = "AssemblyAttributesGoHere";
        internal static readonly string[,] dummyAssemblyAttributeParentQualifier = new string[2, 2] { { "", "M" }, { "S", "SM" } };
        private readonly uint[,] dummyAssemblyAttributeParent = new uint[2, 2] { { 0, 0 }, { 0, 0 } };

        internal const int MappedFieldDataAlignment = 8;
      
        /// <summary>
        /// Wraps a virtual string table index.
        /// An override to SerializeIndex does the resolving at the right time.
        /// </summary>
        private struct StringIdx
        {
            private readonly uint virtIdx;

            internal static readonly StringIdx Empty = new StringIdx(0u);

            internal StringIdx(uint virtIdx)
            {
                this.virtIdx = virtIdx;
            }

            internal uint Resolve(Dictionary<uint, uint> map)
            {
                return map[this.virtIdx];
            }
        }

        /// <summary>
        /// Fills in stringIndexMap with data from stringIndex and write to stringWriter.
        /// Releases stringIndex as the stringTable is sealed after this point.
        /// </summary>
        private void SerializeStringHeap()
        {
            // Sort by suffix and remove stringIndex
            var sorted = new List<KeyValuePair<string, uint>>(this.stringIndex);
            sorted.Sort(new SuffixSort());
            this.stringIndex = null;

            // Create VirtIdx to Idx map and add entry for empty string
            this.stringIndexMap = new Dictionary<uint, uint>(sorted.Count);
            this.stringIndexMap.Add(0, 0);

            // Find strings that can be folded
            string prev = String.Empty;
            foreach (KeyValuePair<string, uint> cur in sorted)
            {
                uint position = this.stringWriter.BaseStream.Position + this.GetStringStreamOffset();

                // It is important to use ordinal comparison otherwise we'll use the current culture!
                if (prev.EndsWith(cur.Key, StringComparison.Ordinal))
                {
                    // Map over the tail of prev string. Watch for null-terminator of prev string.
                    this.stringIndexMap.Add(cur.Value, position - (uint)(Utf8Encoding.GetByteCount(cur.Key) + 1));
                }
                else
                {
                    this.stringIndexMap.Add(cur.Value, position);

                    // TODO (tomat): consider reusing the buffer instead of allocating a new one for each string
                    this.stringWriter.WriteBytes(Utf8Encoding.GetBytes(cur.Key));

                    this.stringWriter.WriteByte(0);
                }

                prev = cur.Key;
            }
        }

        /// <summary>
        /// Sorts strings such that a string is followed immediately by all strings
        /// that are a suffix of it.  
        /// </summary>
        private class SuffixSort : IComparer<KeyValuePair<string, uint>>
        {
            public int Compare(KeyValuePair<string, uint> xPair, KeyValuePair<string, uint> yPair)
            {
                string x = xPair.Key;
                string y = yPair.Key;

                for (int i = x.Length - 1, j = y.Length - 1; i >= 0 & j >= 0; i--, j--)
                {
                    if (x[i] < y[j])
                    {
                        return -1;
                    }

                    if (x[i] > y[j])
                    {
                        return +1;
                    }
                }

                return y.Length.CompareTo(x.Length);
            }
        }

        private ImmutableArray<int> CalculateRowCounts()
        {
            var rowCounts = new int[MetadataTokens.TableCount];

            rowCounts[(int)TableIndex.Assembly] = (this.module.AsAssembly != null) ? 1 : 0;
            rowCounts[(int)TableIndex.AssemblyRef] = this.assemblyRefTable.Count;
            rowCounts[(int)TableIndex.ClassLayout] = this.classLayoutTable.Count;
            rowCounts[(int)TableIndex.Constant] = this.constantTable.Count;
            rowCounts[(int)TableIndex.CustomAttribute] = this.customAttributeTable.Count;
            rowCounts[(int)TableIndex.TypeRef] = this.typeRefTable.Count;
            rowCounts[(int)TableIndex.DeclSecurity] = this.declSecurityTable.Count;
            rowCounts[(int)TableIndex.EncLog] = this.encLogTable.Count;
            rowCounts[(int)TableIndex.EncMap] = this.encMapTable.Count;
            rowCounts[(int)TableIndex.EventMap] = this.eventMapTable.Count;
            rowCounts[(int)TableIndex.Event] = this.eventTable.Count;
            rowCounts[(int)TableIndex.ExportedType] = this.exportedTypeTable.Count;
            rowCounts[(int)TableIndex.FieldLayout] = this.fieldLayoutTable.Count;
            rowCounts[(int)TableIndex.FieldMarshal] = this.fieldMarshalTable.Count;
            rowCounts[(int)TableIndex.FieldRva] = this.fieldRvaTable.Count;
            rowCounts[(int)TableIndex.Field] = this.fieldDefTable.Count;
            rowCounts[(int)TableIndex.File] = this.fileTable.Count;
            rowCounts[(int)TableIndex.GenericParamConstraint] = this.genericParamConstraintTable.Count;
            rowCounts[(int)TableIndex.GenericParam] = this.genericParamTable.Count;
            rowCounts[(int)TableIndex.ImplMap] = this.implMapTable.Count;
            rowCounts[(int)TableIndex.InterfaceImpl] = this.interfaceImplTable.Count;
            rowCounts[(int)TableIndex.ManifestResource] = this.manifestResourceTable.Count;
            rowCounts[(int)TableIndex.MemberRef] = this.memberRefTable.Count;
            rowCounts[(int)TableIndex.MethodImpl] = this.methodImplTable.Count;
            rowCounts[(int)TableIndex.MethodSemantics] = this.methodSemanticsTable.Count;
            rowCounts[(int)TableIndex.MethodSpec] = this.methodSpecTable.Count;
            rowCounts[(int)TableIndex.MethodDef] = this.methodTable.Length;
            rowCounts[(int)TableIndex.ModuleRef] = this.moduleRefTable.Count;
            rowCounts[(int)TableIndex.Module] = 1;
            rowCounts[(int)TableIndex.NestedClass] = this.nestedClassTable.Count;
            rowCounts[(int)TableIndex.Param] = this.paramTable.Count;
            rowCounts[(int)TableIndex.PropertyMap] = this.propertyMapTable.Count;
            rowCounts[(int)TableIndex.Property] = this.propertyTable.Count;
            rowCounts[(int)TableIndex.StandAloneSig] = this.GetStandAloneSignatures().Count;
            rowCounts[(int)TableIndex.TypeDef] = this.typeDefTable.Count;
            rowCounts[(int)TableIndex.TypeRef] = this.typeRefTable.Count;
            rowCounts[(int)TableIndex.TypeSpec] = this.typeSpecTable.Count;

            return ImmutableArray.CreateRange(rowCounts);
        }

        private ImmutableArray<int> CalculateHeapSizes()
        {
            var heapSizes = new int[MetadataTokens.HeapCount];

            heapSizes[(int)HeapIndex.UserString] = (int)this.userStringWriter.BaseStream.Length;
            heapSizes[(int)HeapIndex.String] = (int)this.stringWriter.BaseStream.Length;
            heapSizes[(int)HeapIndex.Blob] = (int)this.blobWriter.BaseStream.Length;
            heapSizes[(int)HeapIndex.Guid] = (int)this.guidWriter.BaseStream.Length;

            return ImmutableArray.CreateRange(heapSizes);
        }

        private void CreateMethodBodyReferenceIndex()
        {
            int count;
            var referencesInIL = module.ReferencesInIL(out count);

            this.pseudoSymbolTokenToTokenMap = new uint[count];
            this.pseudoSymbolTokenToReferenceMap = new IReference[count];

            uint cur = 0;
            foreach (IReference o in referencesInIL)
            {
                pseudoSymbolTokenToReferenceMap[cur] = o;
                cur++;
            }
        }

        private void CreateIndices()
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.CreateUserStringIndices();
            this.CreateInitialAssemblyRefIndex();
            this.CreateInitialFileRefIndex();
            this.CreateIndicesForModule();
            this.CreateInitialExportedTypeIndex();

            // Find all references and assign tokens.
            this.referenceVisitor = this.CreateReferenceVisitor();
            this.module.Dispatch(referenceVisitor);

            this.CreateMethodBodyReferenceIndex();
        }

        private void CreateUserStringIndices()
        {
            this.pseudoStringTokenToStringMap = new List<string>();

            foreach (string str in this.module.GetStrings())
            {
                this.pseudoStringTokenToStringMap.Add(str);
            }

            this.pseudoStringTokenToTokenMap = new uint[pseudoStringTokenToStringMap.Count];
        }

        protected virtual void CreateIndicesForModule()
        {
            var nestedTypes = new Queue<ITypeDefinition>();

            foreach (INamespaceTypeDefinition typeDef in this.GetTopLevelTypes(this.module))
            {
                this.CreateIndicesFor(typeDef, nestedTypes);
            }

            while (nestedTypes.Count > 0)
            {
                this.CreateIndicesFor(nestedTypes.Dequeue(), nestedTypes);
            }
        }

        private void CreateIndicesFor(ITypeDefinition typeDef, Queue<ITypeDefinition> nestedTypes)
        {
            this.cancellationToken.ThrowIfCancellationRequested();

            this.CreateIndicesForNonTypeMembers(typeDef);

            // Metadata spec:
            // The TypeDef table has a special ordering constraint:
            // the definition of an enclosing class shall precede the definition of all classes it encloses.
            foreach (var nestedType in typeDef.GetNestedTypes(Context))
            {
                nestedTypes.Enqueue(nestedType);
            }
        }

        protected IEnumerable<IGenericTypeParameter> GetConsolidatedTypeParameters(ITypeDefinition typeDef)
        {
            INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(Context);
            if (nestedTypeDef == null)
            {
                if (typeDef.IsGeneric)
                {
                    return typeDef.GenericParameters;
                }

                return null;
            }

            return this.GetConsolidatedTypeParameters(typeDef, typeDef);
        }

        private List<IGenericTypeParameter> GetConsolidatedTypeParameters(ITypeDefinition typeDef, ITypeDefinition owner)
        {
            List<IGenericTypeParameter> result = null;
            INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(Context);
            if (nestedTypeDef != null)
            {
                result = this.GetConsolidatedTypeParameters(nestedTypeDef.ContainingTypeDefinition, owner);
            }

            if (typeDef.GenericParameterCount > 0)
            {
                ushort index = 0;
                if (result == null)
                {
                    result = new List<IGenericTypeParameter>();
                }
                else
                {
                    index = (ushort)result.Count;
                }

                if (typeDef == owner && index == 0)
                {
                    result.AddRange(typeDef.GenericParameters);
                }
                else
                {
                    foreach (IGenericTypeParameter genericParameter in typeDef.GenericParameters)
                    {
                        result.Add(new InheritedTypeParameter(index++, owner, genericParameter));
                    }
                }
            }

            return result;
        }

        protected ImmutableArray<IParameterDefinition> GetParametersToEmit(IMethodDefinition methodDef)
        {
            if (methodDef.ParameterCount == 0 && !(methodDef.ReturnValueIsMarshalledExplicitly || IteratorHelper.EnumerableIsNotEmpty(methodDef.ReturnValueAttributes)))
            {
                return ImmutableArray<IParameterDefinition>.Empty;
            }

            return GetParametersToEmitCore(methodDef);
        }

        private ImmutableArray<IParameterDefinition> GetParametersToEmitCore(IMethodDefinition methodDef)
        {
            var builder = ArrayBuilder<IParameterDefinition>.GetInstance();
            if (methodDef.ReturnValueIsMarshalledExplicitly || IteratorHelper.EnumerableIsNotEmpty(methodDef.ReturnValueAttributes))
            {
                builder.Add(new ReturnValueParameter(methodDef));
            }

            foreach (IParameterDefinition parDef in methodDef.Parameters)
            {
                // No explicit param row is needed if param has no flags (other than optionally IN),
                // no name and no references to the param row, such as CustomAttribute, Constant, or FieldMarshal
                if (parDef.HasDefaultValue || parDef.IsOptional || parDef.IsOut || parDef.IsMarshalledExplicitly ||
                    parDef.Name != String.Empty ||
                    IteratorHelper.EnumerableIsNotEmpty(parDef.GetAttributes(Context)))
                {
                    builder.Add(parDef);
                }
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns a reference to the unit that defines the given referenced type. If the referenced type is a structural type, such as a pointer or a generic type instance,
        /// then the result is null.
        /// </summary>
        public static IUnitReference GetDefiningUnitReference(ITypeReference typeReference, EmitContext context)
        {
            INestedTypeReference nestedTypeReference = typeReference.AsNestedTypeReference;
            while (nestedTypeReference != null)
            {
                if (nestedTypeReference.AsGenericTypeInstanceReference != null)
                {
                    return null;
                }

                typeReference = nestedTypeReference.GetContainingType(context);
                nestedTypeReference = typeReference.AsNestedTypeReference;
            }

            INamespaceTypeReference namespaceTypeReference = typeReference.AsNamespaceTypeReference;
            if (namespaceTypeReference == null)
            {
                return null;
            }

            Debug.Assert(namespaceTypeReference.AsGenericTypeInstanceReference == null);

            return namespaceTypeReference.GetUnit(context);
        }

        private void CreateInitialAssemblyRefIndex()
        {
            Debug.Assert(!this.tableIndicesAreComplete);
            foreach (IAssemblyReference assemblyRef in this.module.GetAssemblyReferences(Context))
            {
                this.GetOrAddAssemblyRefIndex(assemblyRef);
            }
        }

        private void CreateInitialExportedTypeIndex()
        {
            Debug.Assert(!this.tableIndicesAreComplete);

            if (this.IsFullMetadata)
            {
                foreach (ITypeExport alias in this.module.GetExportedTypes(Context))
                {
                    ITypeReference exportedType = alias.ExportedType;
                    if (!this.exportedTypeIndex.ContainsKey(exportedType))
                    {
                        this.exportedTypeList.Add(exportedType);
                        this.exportedTypeIndex.Add(exportedType, (uint)this.exportedTypeList.Count);
                    }
                }
            }
        }

        private void CreateInitialFileRefIndex()
        {
            Debug.Assert(!this.tableIndicesAreComplete);
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return;
            }

            foreach (IFileReference fileRef in assembly.GetFiles(Context))
            {
                string key = fileRef.FileName;
                if (!this.fileRefIndex.ContainsKey(key))
                {
                    this.fileRefList.Add(fileRef);
                    this.fileRefIndex.Add(key, (uint)this.fileRefList.Count);
                }
            }
        }

        internal uint GetAssemblyRefIndex(IAssemblyReference assemblyReference)
        {
            var containingAssembly = this.module.GetContainingAssembly(Context);

            if (containingAssembly != null && ReferenceEquals(assemblyReference, containingAssembly))
            {
                return 0;
            }

            return this.GetOrAddAssemblyRefIndex(assemblyReference);
        }

        internal uint GetModuleRefIndex(string moduleName)
        {
            return this.GetOrAddModuleRefIndex(moduleName);
        }

        private uint GetGuidIndex(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return 0;
            }

            uint result;
            if (this.guidIndex.TryGetValue(guid, out result))
            {
                return result;
            }

            Debug.Assert(!this.streamsAreComplete);

            // The only GUIDs that are serialized are MVID, EncId, and EncBaseId in the
            // Module table. Each of those GUID offsets are relative to the local heap,
            // even for deltas, so there's no need for a GetGuidStreamPosition() method
            // to offset the positions by the size of the original heap in delta metadata.
            // Unlike #Blob, #String and #US streams delta #GUID stream is padded to the 
            // size of the previous generation #GUID stream before new GUIDs are added.

            // Metadata Spec: 
            // The Guid heap is an array of GUIDs, each 16 bytes wide. 
            // Its first element is numbered 1, its second 2, and so on.
            result = (this.guidWriter.BaseStream.Length >> 4) + 1;

            this.guidIndex.Add(guid, result);
            this.guidWriter.WriteBytes(guid.ToByteArray());

            return result;
        }

        private uint MakeModuleVersionIdGuidIndex()
        {
            if (this.ModuleVersionId != default(Guid))
            {
                // In the current implementation, we never expect to encounter an IModule
                // that specifies a module ID, but if one did, we would handle it here. 
                return GetGuidIndex(this.ModuleVersionId);
            }
            else if (deterministic)
            {
                // if we are being deterministic, write zero for now and select a Guid later
                uint result = (this.guidWriter.BaseStream.Length >> 4) + 1;
                this.guidWriter.WriteBytes(0, 16);
                return result;
            }
            else
            {
                // If we are being nondeterministic, write a random module version ID guid
                return GetGuidIndex(Guid.NewGuid());
            }
        }

        internal uint GetBlobIndex(MemoryStream stream)
        {
            // TODO: avoid making a copy if the blob exists in the index
            return GetBlobIndex(stream.ToImmutableArray());
        }

        internal uint GetBlobIndex(ImmutableArray<byte> blob)
        {
            uint result = 0;
            if (blob.Length == 0 || this.blobIndex.TryGetValue(blob, out result))
            {
                return result;
            }

            Debug.Assert(!this.streamsAreComplete);
            result = this.blobWriter.BaseStream.Position + this.GetBlobStreamOffset();
            this.blobIndex.Add(blob, result);
            this.blobWriter.WriteCompressedUInt((uint)blob.Length);
            this.blobWriter.WriteBytes(blob);
            return result;
        }

        private uint GetConstantBlobIndex(object value)
        {
            string str = value as string;
            if (str != null)
            {
                return this.GetBlobIndex(str);
            }

            MemoryStream sig = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(sig, true);
            SerializeMetadataConstantValue(value, writer);
            return this.GetBlobIndex(sig);
        }

        private uint GetBlobIndex(string str)
        {
            byte[] byteArray = new byte[str.Length * 2];
            int i = 0;
            foreach (char ch in str)
            {
                byteArray[i++] = (byte)(ch & 0xFF);
                byteArray[i++] = (byte)(ch >> 8);
            }

            return this.GetBlobIndex(ImmutableArray.Create(byteArray));
        }

        private uint GetCustomAttributeSignatureIndex(ICustomAttribute customAttribute)
        {
            uint result = 0;
            if (this.customAtributeSignatureIndex.TryGetValue(customAttribute, out result))
            {
                return result;
            }

            MemoryStream sig = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(sig);
            this.SerializeCustomAttributeSignature(customAttribute, false, writer);
            result = this.GetBlobIndex(sig);
            this.customAtributeSignatureIndex.Add(customAttribute, result);
            return result;
        }

        private uint GetCustomAttributeTypeCodedIndex(IMethodReference methodReference)
        {
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            if (methodDef != null)
            {
                return (this.GetMethodDefIndex(methodDef) << 3) | 2;
            }
            else
            {
                return (this.GetMemberRefIndex(methodReference) << 3) | 3;
            }
        }

        public static ushort GetEventFlags(IEventDefinition eventDef)
        {
            ushort result = 0;
            if (eventDef.IsSpecialName)
            {
                result |= 0x0200;
            }

            if (eventDef.IsRuntimeSpecial)
            {
                result |= 0x0400;
            }

            return result;
        }

        private uint GetExportedTypeIndex(ITypeReference typeReference)
        {
            uint result;
            if (this.exportedTypeIndex.TryGetValue(typeReference, out result))
            {
                return result;
            }

            Debug.Assert(!this.tableIndicesAreComplete);
            this.exportedTypeList.Add(typeReference);
            this.exportedTypeIndex.Add(typeReference, (uint)this.exportedTypeList.Count);
            return result;
        }

        public static ushort GetFieldFlags(IFieldDefinition fieldDef)
        {
            ushort result = GetTypeMemberVisibilityFlags(fieldDef);
            if (fieldDef.IsStatic)
            {
                result |= 0x0010;
            }

            if (fieldDef.IsReadOnly)
            {
                result |= 0x0020;
            }

            if (fieldDef.IsCompileTimeConstant)
            {
                result |= 0x0040;
            }

            if (fieldDef.IsNotSerialized)
            {
                result |= 0x0080;
            }

            if (!fieldDef.MappedData.IsDefault)
            {
                result |= 0x0100;
            }

            if (fieldDef.IsSpecialName)
            {
                result |= 0x0200;
            }

            if (fieldDef.IsRuntimeSpecial)
            {
                result |= 0x0400;
            }

            if (fieldDef.IsMarshalledExplicitly)
            {
                result |= 0x1000;
            }

            if (fieldDef.IsCompileTimeConstant)
            {
                result |= 0x8000;
            }

            return result;
        }

        internal uint GetFieldSignatureIndex(IFieldReference fieldReference)
        {
            uint result = 0;
            ISpecializedFieldReference specializedFieldReference = fieldReference.AsSpecializedFieldReference;
            if (specializedFieldReference != null)
            {
                fieldReference = specializedFieldReference.UnspecializedVersion;
            }

            if (this.fieldSignatureIndex.TryGetValue(fieldReference, out result))
            {
                return result;
            }

            MemoryStream sig = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(sig);
            this.SerializeFieldSignature(fieldReference, writer);
            result = this.GetBlobIndex(sig);
            this.fieldSignatureIndex.Add(fieldReference, result);
            sig.Free();
            return result;
        }

        internal virtual uint GetFieldToken(IFieldReference fieldReference)
        {
            IFieldDefinition fieldDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(fieldReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                fieldDef = fieldReference.GetResolvedField(Context);
            }

            if (fieldDef != null)
            {
                return 0x04000000 | this.GetFieldDefIndex(fieldDef);
            }
            else
            {
                return 0x0A000000 | this.GetMemberRefIndex(fieldReference);
            }
        }

        internal uint GetFileRefIndex(IFileReference fileReference)
        {
            string key = fileReference.FileName;
            uint result;
            if (this.fileRefIndex.TryGetValue(key, out result))
            {
                return result;
            }

            Debug.Assert(!this.tableIndicesAreComplete);
            this.fileRefList.Add(fileReference);
            this.fileRefIndex.Add(key, (uint)this.fileRefList.Count);
            return result;
        }

        private uint GetFileRefIndex(IModuleReference mref)
        {
            string key = mref.Name;
            uint result = 0;
            if (this.fileRefIndex.TryGetValue(key, out result))
            {
                return result;
            }

            Debug.Assert(false);

            // TODO: error
            return result;
        }

        private static ushort GetGenericParamFlags(IGenericParameter genPar)
        {
            ushort result = 0;
            switch (genPar.Variance)
            {
                case TypeParameterVariance.Covariant:
                    result |= 0x0001;
                    break;
                case TypeParameterVariance.Contravariant:
                    result |= 0x0002;
                    break;
            }

            if (genPar.MustBeReferenceType)
            {
                result |= 0x0004;
            }

            if (genPar.MustBeValueType)
            {
                result |= 0x0008;
            }

            if (genPar.MustHaveDefaultConstructor)
            {
                result |= 0x0010;
            }

            return result;
        }

        private uint GetImplementationCodedIndex(INamespaceTypeReference namespaceRef)
        {
            IUnitReference uref = namespaceRef.GetUnit(Context);
            IAssemblyReference aref = uref as IAssemblyReference;
            if (aref != null)
            {
                return (this.GetAssemblyRefIndex(aref) << 2) | 1;
            }

            IModuleReference mref = uref as IModuleReference;
            if (mref != null)
            {
                aref = mref.GetContainingAssembly(Context);
                if (aref == null || ReferenceEquals(aref, this.module.GetContainingAssembly(Context)))
                {
                    return (this.GetFileRefIndex(mref) << 2) | 0;
                }
                else
                {
                    return (this.GetAssemblyRefIndex(aref) << 2) | 1;
                }
            }

            Debug.Assert(false);

            // TODO: error
            return 0;
        }

        private uint GetManagedResourceOffset(ManagedResource resource, BinaryWriter resourceWriter)
        {
            Debug.Assert(!this.streamsAreComplete);
            if (resource.ExternalFile != null)
            {
                return resource.Offset;
            }

            uint result = resourceWriter.BaseStream.Position;
            resource.WriteData(resourceWriter);
            return result;
        }

        public static string GetMangledName(INamedTypeReference namedType)
        {
            string unmangledName = namedType.Name;

            return namedType.MangleName
                ? MetadataHelpers.ComposeAritySuffixedMetadataName(unmangledName, namedType.GenericParameterCount)
                : unmangledName;
        }

        private static string GetMangledAndEscapedName(INamedTypeReference namedType)
        {
            string needsEscaping = "\\[]*.+,& ";
            StringBuilder mangledName = new StringBuilder();
            foreach (var ch in namedType.Name)
            {
                if (needsEscaping.IndexOf(ch) >= 0)
                {
                    mangledName.Append('\\');
                }

                mangledName.Append(ch);
            }

            if (namedType.MangleName && namedType.GenericParameterCount > 0)
            {
                mangledName.Append(MetadataHelpers.GetAritySuffix(namedType.GenericParameterCount));
            }

            return mangledName.ToString();
        }

        internal uint GetMemberRefIndex(ITypeMemberReference memberRef)
        {
            return this.GetOrAddMemberRefIndex(memberRef);
        }

        internal uint GetMemberRefParentCodedIndex(ITypeMemberReference memberRef)
        {
            ITypeDefinition parentTypeDef = memberRef.GetContainingType(Context).AsTypeDefinition(Context);
            if (parentTypeDef != null)
            {
                uint parentTypeDefIndex = 0;
                this.TryGetTypeDefIndex(parentTypeDef, out parentTypeDefIndex);
                if (parentTypeDefIndex > 0)
                {
                    IFieldReference fieldRef = memberRef as IFieldReference;
                    if (fieldRef != null)
                    {
                        return parentTypeDefIndex << 3;
                    }

                    IMethodReference methodRef = memberRef as IMethodReference;
                    if (methodRef != null)
                    {
                        if (methodRef.AcceptsExtraArguments)
                        {
                            uint methodIndex = 0;
                            if (this.TryGetMethodDefIndex(methodRef.GetResolvedMethod(Context), out methodIndex))
                            {
                                return (methodIndex << 3) | 3;
                            }
                        }

                        return parentTypeDefIndex << 3;
                    }

                    // TODO: error
                }
            }

            // TODO: special treatment for global fields and methods. Object model support would be nice.
            if (!IsTypeSpecification(memberRef.GetContainingType(Context)))
            {
                return (this.GetTypeRefIndex(memberRef.GetContainingType(Context)) << 3) | 1;
            }
            else
            {
                return (this.GetTypeSpecIndex(memberRef.GetContainingType(Context)) << 3) | 4;
            }
        }

        private static bool IsTypeSpecification(ITypeReference typeReference)
        {
            INestedTypeReference nestedTypeReference = typeReference.AsNestedTypeReference;
            if (nestedTypeReference != null)
            {
                return nestedTypeReference.AsSpecializedNestedTypeReference != null ||
                    nestedTypeReference.AsGenericTypeInstanceReference != null;
            }

            return typeReference.AsNamespaceTypeReference == null;
        }

        internal uint GetMethodDefOrRefCodedIndex(IMethodReference methodReference)
        {
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            if (methodDef != null)
            {
                return this.GetMethodDefIndex(methodDef) << 1;
            }
            else
            {
                return (this.GetMemberRefIndex(methodReference) << 1) | 1;
            }
        }

        public static ushort GetMethodFlags(IMethodDefinition methodDef)
        {
            ushort result = GetTypeMemberVisibilityFlags(methodDef);
            if (methodDef.IsStatic)
            {
                result |= 0x0010;
            }

            if (methodDef.IsSealed)
            {
                result |= 0x0020;
            }

            if (methodDef.IsVirtual)
            {
                result |= 0x0040;
            }

            if (methodDef.IsHiddenBySignature)
            {
                result |= 0x0080;
            }

            if (methodDef.IsNewSlot)
            {
                result |= 0x0100;
            }

            if (methodDef.IsAccessCheckedOnOverride)
            {
                result |= 0x0200;
            }

            if (methodDef.IsAbstract)
            {
                result |= 0x0400;
            }

            if (methodDef.IsSpecialName)
            {
                result |= 0x0800;
            }

            if (methodDef.IsRuntimeSpecial)
            {
                result |= 0x1000;
            }

            if (methodDef.IsPlatformInvoke)
            {
                result |= 0x2000;
            }

            if (methodDef.HasDeclarativeSecurity)
            {
                result |= 0x4000;
            }

            if (methodDef.RequiresSecurityObject)
            {
                result |= 0x8000;
            }

            return result;
        }

        internal uint GetMethodInstanceSignatureIndex(IGenericMethodInstanceReference methodInstanceReference)
        {
            uint result = 0;
            if (this.methodInstanceSignatureIndex.TryGetValue(methodInstanceReference, out result))
            {
                return result;
            }

            MemoryStream sig = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(sig);
            writer.WriteByte(0x0A);
            writer.WriteCompressedUInt(methodInstanceReference.GetGenericMethod(Context).GenericParameterCount);
            foreach (ITypeReference typeref in methodInstanceReference.GetGenericArguments(Context))
            {
                this.SerializeTypeReference(typeref, writer, false, true);
            }

            result = this.GetBlobIndex(sig);
            this.methodInstanceSignatureIndex.Add(methodInstanceReference, result);
            sig.Free();
            return result;
        }

        private uint GetMarshallingDescriptorIndex(IMarshallingInformation marshallingInformation)
        {
            uint result = 0;
            if (this.marshallingDescriptorIndex.TryGetValue(marshallingInformation, out result))
            {
                return result;
            }

            MemoryStream sig = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(sig);
            this.SerializeMarshallingDescriptor(marshallingInformation, writer);
            result = this.GetBlobIndex(sig);
            this.marshallingDescriptorIndex.Add(marshallingInformation, result);
            sig.Free();
            return result;
        }

        private uint GetMarshallingDescriptorIndex(ImmutableArray<byte> descriptor)
        {
            return this.GetBlobIndex(descriptor);
        }

        private uint GetMemberRefSignatureIndex(ITypeMemberReference memberRef)
        {
            IFieldReference fieldReference = memberRef as IFieldReference;
            if (fieldReference != null)
            {
                return this.GetFieldSignatureIndex(fieldReference);
            }

            IMethodReference methodReference = memberRef as IMethodReference;
            if (methodReference != null)
            {
                return this.GetMethodSignatureIndex(methodReference);
            }            // TODO: error

            return 0;
        }

        internal uint GetMethodSignatureIndex(IMethodReference methodReference)
        {
            uint result = 0;
            ISpecializedMethodReference specializedMethodReference = methodReference.AsSpecializedMethodReference;
            if (specializedMethodReference != null)
            {
                methodReference = specializedMethodReference.UnspecializedVersion;
            }

            if (this.signatureIndex.TryGetValue(methodReference, out result))
            {
                return result;
            }

            MemoryStream sig = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(sig);
            this.SerializeSignature(methodReference, methodReference.GenericParameterCount, methodReference.ExtraParameters, writer);
            result = this.GetBlobIndex(sig);
            this.signatureIndex.Add(methodReference, result);
            sig.Free();
            return result;
        }

        unsafe internal byte[] GetMethodSignature(IMethodReference methodReference)
        {
            int signatureOffset = (int)GetMethodSignatureIndex(methodReference);

            fixed (byte* ptr = this.blobWriter.BaseStream.Buffer)
            {
                var reader = new BlobReader(ptr + signatureOffset, (int)this.blobWriter.BaseStream.Length + (int)this.GetBlobStreamOffset() - signatureOffset);
                int size;
                bool isValid = reader.TryReadCompressedInteger(out size);
                Debug.Assert(isValid);
                return reader.ReadBytes(size);
            }
        }

        private uint GetGenericMethodInstanceIndex(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            MemoryStream sig = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(sig);
            this.SerializeGenericMethodInstanceSignature(writer, genericMethodInstanceReference);
            uint result = this.GetBlobIndex(sig);
            sig.Free();
            return result;
        }

        private uint GetMethodSpecIndex(IGenericMethodInstanceReference methodSpec)
        {
            return this.GetOrAddMethodSpecIndex(methodSpec);
        }

        internal virtual uint GetMethodToken(IMethodReference methodReference)
        {
            uint methodDefIndex = 0;
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            if (methodDef != null && (methodReference == methodDef || !methodReference.AcceptsExtraArguments) && this.TryGetMethodDefIndex(methodDef, out methodDefIndex))
            {
                return 0x06000000 | methodDefIndex;
            }
            else
            {
                IGenericMethodInstanceReference methodSpec = methodReference.AsGenericMethodInstanceReference;
                if (methodSpec != null)
                {
                    return 0x2B000000 | this.GetMethodSpecIndex(methodSpec);
                }
                else
                {
                    return 0x0A000000 | this.GetMemberRefIndex(methodReference);
                }
            }
        }

        public static ushort GetParameterFlags(IParameterDefinition parDef)
        {
            ushort result = 0;
            if (parDef.IsIn)
            {
                result |= 0x0001;
            }

            if (parDef.IsOut)
            {
                result |= 0x0002;
            }

            if (parDef.IsOptional)
            {
                result |= 0x0010;
            }

            if (parDef.HasDefaultValue)
            {
                result |= 0x1000;
            }

            if (parDef.IsMarshalledExplicitly)
            {
                result |= 0x2000;
            }

            return result;
        }

        internal PrimitiveTypeCode GetConstantTypeCode(ILocalDefinition constant)
        {
            return constant.CompileTimeValue.Type.TypeCode(Context);
        }

        private uint GetPermissionSetIndex(ImmutableArray<ICustomAttribute> permissionSet)
        {
            MemoryStream sig = MemoryStream.GetInstance();
            uint result = 0;
            try
            {
                BinaryWriter writer = new BinaryWriter(sig);
                writer.WriteByte((byte)'.');
                writer.WriteCompressedUInt((uint)permissionSet.Length);
                this.SerializePermissionSet(permissionSet, writer);
                result = this.GetBlobIndex(sig);
            }
            finally
            {
                sig.Free();
            }

            return result;
        }

        public static ushort GetPropertyFlags(IPropertyDefinition propertyDef)
        {
            ushort result = 0;
            if (propertyDef.IsSpecialName)
            {
                result |= 0x0200;
            }

            if (propertyDef.IsRuntimeSpecial)
            {
                result |= 0x0400;
            }

            if (propertyDef.HasDefaultValue)
            {
                result |= 0x1000;
            }

            return result;
        }

        private uint GetPropertySignatureIndex(IPropertyDefinition propertyDef)
        {
            uint result = 0;
            if (this.signatureIndex.TryGetValue(propertyDef, out result))
            {
                return result;
            }

            MemoryStream sig = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(sig);
            this.SerializeSignature(propertyDef, 0, ImmutableArray<IParameterTypeInformation>.Empty, writer);
            result = this.GetBlobIndex(sig);
            this.signatureIndex.Add(propertyDef, result);
            sig.Free();
            return result;
        }

        private uint GetResolutionScopeCodedIndex(ITypeReference typeReference)
        {
            return (this.GetTypeRefIndex(typeReference) << 2) | 3;
        }

        private uint GetResolutionScopeCodedIndex(IUnitReference unitReference)
        {
            IAssemblyReference aref = unitReference as IAssemblyReference;
            if (aref != null)
            {
                return (this.GetAssemblyRefIndex(aref) << 2) | 2;
            }

            IModuleReference mref = unitReference as IModuleReference;
            if (mref != null)
            {
                // If this is a module from a referenced multi-module assembly,
                // the assembly should be used as the resolution scope.
                aref = mref.GetContainingAssembly(Context);

                if (aref != null && aref != module.AsAssembly)
                {
                    return (this.GetAssemblyRefIndex(aref) << 2) | 2;
                }

                return (this.GetModuleRefIndex(mref.Name) << 2) | 1;
            }

            // TODO: error
            return 0;
        }

        private StringIdx GetStringIndex(string str)
        {
            uint index = 0;
            if (str.Length > 0 && !this.stringIndex.TryGetValue(str, out index))
            {
                Debug.Assert(!this.streamsAreComplete);
                index = (uint)this.stringIndex.Count + 1; // idx 0 is reserved for empty string
                this.stringIndex.Add(str, index);
            }

            return new StringIdx(index);
        }

        private StringIdx GetStringIndexForPathAndCheckLength(string path, INamedEntity errorEntity = null)
        {
            CheckPathLength(path, errorEntity);
            return GetStringIndex(path);
        }

        private StringIdx GetStringIndexForNameAndCheckLength(string name, INamedEntity errorEntity = null)
        {
            CheckNameLength(name, errorEntity);
            return GetStringIndex(name);
        }

        /// <summary>
        /// The Microsoft CLR requires that {namespace} + "." + {name} fit in MAX_CLASS_NAME 
        /// (even though the name and namespace are stored separately in the Microsoft
        /// implementation).  Note that the namespace name of a nested type is always blank
        /// (since comes from the container).
        /// </summary>
        /// <param name="namespaceType">We're trying to add the containing namespace of this type to the string heap.</param>
        /// <param name="mangledTypeName">Namespace names are never used on their own - this is the type that is adding the namespace name.
        /// Used only for length checking.</param>
        private StringIdx GetStringIndexForNamespaceAndCheckLength(INamespaceTypeReference namespaceType, string mangledTypeName)
        {
            string namespaceName = namespaceType.NamespaceName;
            if (namespaceName.Length == 0) // Optimization: CheckNamespaceLength is relatively expensive.
            {
                return StringIdx.Empty;
            }

            CheckNamespaceLength(namespaceName, mangledTypeName, namespaceType);
            return GetStringIndex(namespaceName);
        }

        private void CheckNameLength(string name, INamedEntity errorEntity)
        {
            // NOTE: ildasm shows quotes around some names (e.g. explicit implementations of members of generic interfaces)
            // but that seems to be tool-specific - they don't seem to and up in the string heap (so they don't count against
            // the length limit).

            if (IsTooLongInternal(name, NameLengthLimit))
            {
                Location location = GetNamedEntityLocation(errorEntity);
                this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.ERR_MetadataNameTooLong, location, name));
            }
        }

        private void CheckPathLength(string path, INamedEntity errorEntity = null)
        {
            if (IsTooLongInternal(path, PathLengthLimit))
            {
                Location location = GetNamedEntityLocation(errorEntity);
                this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.ERR_MetadataNameTooLong, location, path));
            }
        }

        private void CheckNamespaceLength(string namespaceName, string mangledTypeName, INamespaceTypeReference errorEntity)
        {
            // It's never useful to report that the namespace name is too long.
            // If it's too long, then the full name is too long and that string is
            // more helpful.

            // PERF: We expect to check this A LOT, so we'll aggressively inline some
            // of the helpers (esp IsTooLongInternal) in a way that allows us to forego
            // string concatenation (unless a diagnostic is actually reported).

            if (namespaceName.Length + 1 + mangledTypeName.Length > NameLengthLimit / 3)
            {
                int utf8Length =
                    Utf8Encoding.GetByteCount(namespaceName) +
                    1 + // dot
                    Utf8Encoding.GetByteCount(mangledTypeName);

                if (utf8Length > NameLengthLimit)
                {
                    Location location = GetNamedEntityLocation(errorEntity);
                    this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.ERR_MetadataNameTooLong, location, namespaceName + "." + mangledTypeName));
                }
            }
        }

        internal bool IsUsingStringTooLong(string usingString, INamedEntity errorEntity = null)
        {
            if (IsTooLongInternal(usingString, PdbLengthLimit))
            {
                Location location = GetNamedEntityLocation(errorEntity);
                this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.WRN_PdbUsingNameTooLong, location, usingString));
                return true;
            }

            return false;
        }

        internal bool IsLocalNameTooLong(ILocalDefinition localDefinition)
        {
            string name = localDefinition.Name;
            if (IsTooLongInternal(name, PdbLengthLimit))
            {
                this.Context.Diagnostics.Add(this.messageProvider.CreateDiagnostic(this.messageProvider.WRN_PdbLocalNameTooLong, localDefinition.Location, name));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Test the given name to see if it fits in metadata.
        /// </summary>
        /// <param name="str">String to test (non-null).</param>
        /// <param name="maxLength">Max length for name.  (Expected to be at least 5.)</param>
        /// <returns>True if the name is too long.</returns>
        /// <remarks>Internal for test purposes.</remarks>
        internal static bool IsTooLongInternal(string str, int maxLength)
        {
            Debug.Assert(str != null); // No need to handle in an internal utility.

            if (str.Length < maxLength / 3) //UTF-8 uses at most 3 bytes per char
            {
                return false;
            }

            int utf8Length = Utf8Encoding.GetByteCount(str);
            return utf8Length > maxLength;
        }

        private static Location GetNamedEntityLocation(INamedEntity errorEntity)
        {
            return GetSymbolLocation(errorEntity as ISymbol);
        }

        protected static Location GetSymbolLocation(ISymbol symbolOpt)
        {
            return symbolOpt != null && !symbolOpt.Locations.IsDefaultOrEmpty ? symbolOpt.Locations[0] : Location.None;
        }

        private static SignatureTypeCode GetConstantTypeCode(object val)
        {
            if (val == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a zero.
                return Constants.SignatureTypeCode_Class;
            }

            Debug.Assert(!val.GetType().GetTypeInfo().IsEnum);

            // Perf: Note that JIT optimizes each expression val.GetType() == typeof(T) to a single register comparison.
            // Also the checks are sorted by commonality of the checked types.

            if (val.GetType() == typeof(int))
            {
                return SignatureTypeCode.Int32;
            }

            if (val.GetType() == typeof(string))
            {
                return SignatureTypeCode.String;
            }

            if (val.GetType() == typeof(bool))
            {
                return SignatureTypeCode.Boolean;
            }

            if (val.GetType() == typeof(char))
            {
                return SignatureTypeCode.Char;
            }

            if (val.GetType() == typeof(byte))
            {
                return SignatureTypeCode.Byte;
            }

            if (val.GetType() == typeof(long))
            {
                return SignatureTypeCode.Int64;
            }

            if (val.GetType() == typeof(double))
            {
                return SignatureTypeCode.Double;
            }

            if (val.GetType() == typeof(short))
            {
                return SignatureTypeCode.Int16;
            }

            if (val.GetType() == typeof(ushort))
            {
                return SignatureTypeCode.UInt16;
            }

            if (val.GetType() == typeof(uint))
            {
                return SignatureTypeCode.UInt32;
            }

            if (val.GetType() == typeof(sbyte))
            {
                return SignatureTypeCode.SByte;
            }

            if (val.GetType() == typeof(ulong))
            {
                return SignatureTypeCode.UInt64;
            }

            if (val.GetType() == typeof(float))
            {
                return SignatureTypeCode.Single;
            }

            throw ExceptionUtilities.UnexpectedValue(val);
        }

        internal uint GetTypeDefFlags(ITypeDefinition typeDef)
        {
            return GetTypeDefFlags(typeDef, Context);
        }

        public static uint GetTypeDefFlags(ITypeDefinition typeDef, EmitContext context)
        {
            TypeAttributes result = default(TypeAttributes);

            switch (typeDef.Layout)
            {
                case LayoutKind.Sequential:
                    result |= TypeAttributes.SequentialLayout;
                    break;

                case LayoutKind.Explicit:
                    result |= TypeAttributes.ExplicitLayout;
                    break;
            }

            if (typeDef.IsInterface)
            {
                result |= TypeAttributes.Interface;
            }

            if (typeDef.IsAbstract)
            {
                result |= TypeAttributes.Abstract;
            }

            if (typeDef.IsSealed)
            {
                result |= TypeAttributes.Sealed;
            }

            if (typeDef.IsSpecialName)
            {
                result |= TypeAttributes.SpecialName;
            }

            if (typeDef.IsRuntimeSpecial)
            {
                result |= TypeAttributes.RTSpecialName;
            }

            if (typeDef.IsComObject)
            {
                result |= TypeAttributes.Import;
            }

            if (typeDef.IsSerializable)
            {
                result |= TypeAttributes.Serializable;
            }

            if (typeDef.IsWindowsRuntimeImport)
            {
                result |= TypeAttributes.WindowsRuntime;
            }

            switch (typeDef.StringFormat)
            {
                case CharSet.Unicode:
                    result |= TypeAttributes.UnicodeClass;
                    break;

                case Constants.CharSet_Auto:
                    result |= TypeAttributes.AutoClass;
                    break;
            }

            if (typeDef.HasDeclarativeSecurity)
            {
                result |= TypeAttributes.HasSecurity;
            }

            if (typeDef.IsBeforeFieldInit)
            {
                result |= TypeAttributes.BeforeFieldInit;
            }

            INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(context);
            if (nestedTypeDef != null)
            {
                switch (((ITypeDefinitionMember)typeDef).Visibility)
                {
                    case TypeMemberVisibility.Public:
                        result |= TypeAttributes.NestedPublic;
                        break;
                    case TypeMemberVisibility.Private:
                        result |= TypeAttributes.NestedPrivate;
                        break;
                    case TypeMemberVisibility.Family:
                        result |= TypeAttributes.NestedFamily;
                        break;
                    case TypeMemberVisibility.Assembly:
                        result |= TypeAttributes.NestedAssembly;
                        break;
                    case TypeMemberVisibility.FamilyAndAssembly:
                        result |= TypeAttributes.NestedFamANDAssem;
                        break;
                    case TypeMemberVisibility.FamilyOrAssembly:
                        result |= TypeAttributes.NestedFamORAssem;
                        break;
                }

                return (uint)result;
            }

            INamespaceTypeDefinition namespaceTypeDef = typeDef.AsNamespaceTypeDefinition(context);
            if (namespaceTypeDef != null && namespaceTypeDef.IsPublic)
            {
                result |= TypeAttributes.Public;
            }

            return (uint)result;
        }

        private uint GetTypeDefOrRefCodedIndex(ITypeReference typeReference, bool treatRefAsPotentialTypeSpec)
        {
            uint typeDefIndex = 0;
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            if ((typeDefinition != null) && this.TryGetTypeDefIndex(typeDefinition, out typeDefIndex))
            {
                return (typeDefIndex << 2) | 0;
            }

            if (!treatRefAsPotentialTypeSpec || !IsTypeSpecification(typeReference))
            {
                return (this.GetTypeRefIndex(typeReference) << 2) | 1;
            }
            else
            {
                return (this.GetTypeSpecIndex(typeReference) << 2) | 2;
            }
        }

        private static ushort GetTypeMemberVisibilityFlags(ITypeDefinitionMember member)
        {
            ushort result = 0;
            switch (member.Visibility)
            {
                case TypeMemberVisibility.Private:
                    result |= 0x00000001;
                    break;
                case TypeMemberVisibility.FamilyAndAssembly:
                    result |= 0x00000002;
                    break;
                case TypeMemberVisibility.Assembly:
                    result |= 0x00000003;
                    break;
                case TypeMemberVisibility.Family:
                    result |= 0x00000004;
                    break;
                case TypeMemberVisibility.FamilyOrAssembly:
                    result |= 0x00000005;
                    break;
                case TypeMemberVisibility.Public:
                    result |= 0x00000006;
                    break;
            }

            return result;
        }

        private uint GetTypeOrMethodDefCodedIndex(IGenericParameter genPar)
        {
            IGenericTypeParameter genTypePar = genPar.AsGenericTypeParameter;
            if (genTypePar != null)
            {
                return this.GetTypeDefIndex(genTypePar.DefiningType) << 1;
            }

            IGenericMethodParameter genMethPar = genPar.AsGenericMethodParameter;
            if (genMethPar != null)
            {
                return (this.GetMethodDefIndex(genMethPar.DefiningMethod) << 1) | 1;
            }            // TODO: error

            return 0;
        }

        private uint GetTypeRefIndex(ITypeReference typeReference)
        {
            uint result;
            if (this.TryGetTypeRefIndex(typeReference, out result))
            {
                return result;
            }

            // NOTE: Even though CLR documentation does not explicitly specify any requirements 
            // NOTE: to the order of records in TypeRef table, some tools and/or APIs (e.g. 
            // NOTE: IMetaDataEmit::MergeEnd) assume that the containing type referenced as 
            // NOTE: ResolutionScope for its nested types should appear in TypeRef table
            // NOTE: *before* any of its nested types.
            // SEE ALSO: bug#570975 and test Bug570975()
            INestedTypeReference nestedTypeRef = typeReference.AsNestedTypeReference;
            if (nestedTypeRef != null)
            {
                GetTypeRefIndex(nestedTypeRef.GetContainingType(this.Context));
            }

            return this.GetOrAddTypeRefIndex(typeReference);
        }

        private uint GetTypeSpecIndex(ITypeReference typeReference)
        {
            return this.GetOrAddTypeSpecIndex(typeReference);
        }

        internal ITypeDefinition GetTypeDefinition(uint token)
        {
            // The token must refer to a TypeDef row since we are
            // only handling indexes into the full metadata (in EnC)
            // for def tables. Other tables contain deltas only.
            Debug.Assert(TypeOnly(token) == TokenTypeIds.TypeDef);
            int index = (int)RowOnly(token) - 1;
            return this.GetTypeDef(index);
        }

        internal IMethodDefinition GetMethodDefinition(uint token)
        {
            // Must be a def table. (See comment in GetTypeDefinition.)
            Debug.Assert(TypeOnly(token) == TokenTypeIds.MethodDef);
            int index = (int)RowOnly(token) - 1;
            return this.GetMethodDef(index);
        }

        internal INestedTypeReference GetNestedTypeReference(uint token)
        {
            // Must be a def table. (See comment in GetTypeDefinition.)
            Debug.Assert(TypeOnly(token) == TokenTypeIds.TypeDef);
            int index = (int)RowOnly(token) - 1;
            var t = this.GetTypeDef(index);
            return t.AsNestedTypeReference;
        }

        internal uint GetTypeSpecSignatureIndex(ITypeReference typeReference)
        {
            uint result = 0;
            if (this.typeSpecSignatureIndex.TryGetValue(typeReference, out result))
            {
                return result;
            }

            MemoryStream sig = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(sig);
            this.SerializeTypeReference(typeReference, writer, false, true);
            result = this.GetBlobIndex(sig);
            this.typeSpecSignatureIndex.Add(typeReference, result);
            sig.Free();
            return result;
        }

        internal void RecordTypeReference(ITypeReference typeReference)
        {
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            uint token;
            if ((typeDefinition != null) && this.TryGetTypeDefIndex(typeDefinition, out token))
            {
                return;
            }

            if (!IsTypeSpecification(typeReference))
            {
                this.GetTypeRefIndex(typeReference);
            }
            else
            {
                this.GetTypeSpecIndex(typeReference);
            }
        }

        internal virtual uint GetTypeToken(ITypeReference typeReference)
        {
            uint typeDefIndex = 0;
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            if ((typeDefinition != null) && this.TryGetTypeDefIndex(typeDefinition, out typeDefIndex))
            {
                return 0x02000000 | typeDefIndex;
            }

            if (!IsTypeSpecification(typeReference))
            {
                return 0x01000000 | this.GetTypeRefIndex(typeReference);
            }
            else
            {
                return 0x1B000000 | this.GetTypeSpecIndex(typeReference);
            }
        }

        internal uint GetTokenForDefinition(IDefinition definition)
        {
            ITypeDefinition typeDef = definition as ITypeDefinition;
            if (typeDef != null)
            {
                return TokenTypeIds.TypeDef | this.GetTypeDefIndex(typeDef);
            }

            IMethodDefinition methodDef = definition as IMethodDefinition;
            if (methodDef != null)
            {
                return TokenTypeIds.MethodDef | this.GetMethodDefIndex(methodDef);
            }

            IFieldDefinition fieldDef = definition as IFieldDefinition;
            if (fieldDef != null)
            {
                return TokenTypeIds.FieldDef | this.GetFieldDefIndex(fieldDef);
            }

            IEventDefinition eventDef = definition as IEventDefinition;
            if (eventDef != null)
            {
                return TokenTypeIds.Event | this.GetEventDefIndex(eventDef);
            }

            IPropertyDefinition propertyDef = definition as IPropertyDefinition;
            if (propertyDef != null)
            {
                return TokenTypeIds.Property | this.GetPropertyDefIndex(propertyDef);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private uint GetUserStringToken(string str)
        {
            uint index;
            if (!this.userStringIndex.TryGetValue(str, out index))
            {
                Debug.Assert(!this.streamsAreComplete);
                index = this.userStringWriter.BaseStream.Position + this.GetUserStringStreamOffset();
                this.userStringIndex.Add(str, index);
                this.userStringWriter.WriteCompressedUInt((uint)str.Length * 2 + 1);
                this.userStringWriter.WriteChars(str.ToCharArray());

                // Write out a trailing byte indicating if the string is really quite simple
                byte stringKind = 0;
                foreach (char ch in str)
                {
                    if (ch >= 0x7F)
                    {
                        stringKind = 1;
                    }
                    else
                    {
                        switch ((int)ch)
                        {
                            case 0x1:
                            case 0x2:
                            case 0x3:
                            case 0x4:
                            case 0x5:
                            case 0x6:
                            case 0x7:
                            case 0x8:
                            case 0xE:
                            case 0xF:
                            case 0x10:
                            case 0x11:
                            case 0x12:
                            case 0x13:
                            case 0x14:
                            case 0x15:
                            case 0x16:
                            case 0x17:
                            case 0x18:
                            case 0x19:
                            case 0x1A:
                            case 0x1B:
                            case 0x1C:
                            case 0x1D:
                            case 0x1E:
                            case 0x1F:
                            case 0x27:
                            case 0x2D:
                                stringKind = 1;
                                break;
                            default:
                                continue;
                        }
                    }

                    break;
                }

                this.userStringWriter.WriteByte(stringKind);
            }

            return 0x70000000 | index;
        }

        private void SerializeCustomModifier(ICustomModifier customModifier, BinaryWriter writer)
        {
            if (customModifier.IsOptional)
            {
                writer.WriteByte(0x20);
            }
            else
            {
                writer.WriteByte(0x1f);
            }

            writer.WriteCompressedUInt(this.GetTypeDefOrRefCodedIndex(customModifier.GetModifier(Context), true));
        }

        private void SerializeMetadataHeader(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            uint startOffset = writer.BaseStream.Position;

            // Storage signature
            writer.WriteUint(0x424A5342); // Signature 4
            writer.WriteUshort(1); // metadata version major 6
            writer.WriteUshort(1); // metadata version minor 8
            writer.WriteUint(0); // reserved 12
            writer.WriteUint(12); // version must be 12 chars long (TODO: this observation is not supported by the standard or the ILAsm book). 16
            string targetRuntimeVersion = this.module.TargetRuntimeVersion;
            int n = targetRuntimeVersion.Length;
            for (int i = 0; i < 12 && i < n; i++)
            {
                writer.WriteByte((byte)targetRuntimeVersion[i]);
            }

            for (int i = n; i < 12; i++)
            {
                writer.WriteByte(0); // 28
            }

            // Storage header
            writer.WriteByte(0); // flags 29
            writer.WriteByte(0); // padding 30
            writer.WriteUshort((ushort)(this.IsMinimalDelta ? 6 : 5)); // number of streams 32

            // Stream headers
            int offsetFromStartOfMetadata = metadataSizes.MetadataHeaderSize;
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.MetadataTableStreamSize, (this.CompressMetadataStream ? "#~" : "#-"), writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.String), "#Strings", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.UserString), "#US", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.Guid), "#GUID", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, metadataSizes.GetAlignedHeapSize(HeapIndex.Blob), "#Blob", writer);
            if (this.IsMinimalDelta)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, 0, "#JTD", writer);
            }

            uint endOffset = writer.BaseStream.Position;
            Debug.Assert(endOffset - startOffset == metadataSizes.MetadataHeaderSize);
        }

        private static void SerializeStreamHeader(ref int offsetFromStartOfMetadata, int alignedStreamSize, string streamName, BinaryWriter writer)
        {
            // 4 for the first uint (offset), 4 for the second uint (padded size), length of stream name + 1 for null terminator (then padded)
            int sizeOfStreamHeader = 8 + BitArithmeticUtilities.Align(streamName.Length + 1, 4);
            writer.WriteInt(offsetFromStartOfMetadata);
            writer.WriteInt(alignedStreamSize);
            foreach (char ch in streamName)
            {
                writer.WriteByte((byte)ch);
            }

            // After offset, size, and stream name, write 0-bytes until we reach our padded size.
            for (uint i = 8 + (uint)streamName.Length; i < sizeOfStreamHeader; i++)
            {
                writer.WriteByte(0);
            }

            offsetFromStartOfMetadata += alignedStreamSize;
        }

        public void WriteMetadataAndIL(PdbWriter pdbWriterOpt, Stream metadataStream, Stream ilStream, out MetadataSizes metadataSizes)
        {
            if (pdbWriterOpt != null)
            {
                pdbWriterOpt.SetMetadataEmitter(this);
            }

            // TODO: we can precalculate the exact size of IL stream
            var ilBuffer = new MemoryStream(1024);
            var ilWriter = new BinaryWriter(ilBuffer);
            var metadataBuffer = new MemoryStream(4 * 1024);
            var metadataWriter = new BinaryWriter(metadataBuffer);
            var mappedFieldDataBuffer = new MemoryStream(0);
            var mappedFieldDataWriter = new BinaryWriter(mappedFieldDataBuffer);
            var managedResourceDataBuffer = new MemoryStream(0);
            var managedResourceDataWriter = new BinaryWriter(managedResourceDataBuffer);

            // Add 4B of padding to the start of the separated IL stream, 
            // so that method RVAs, which are offsets to this stream, are never 0.
            ilWriter.WriteUint(0);

            // this is used to handle edit-and-continue emit, so we should have a module
            // version ID that is imposed by the caller (the same as the previous module version ID).
            // Therefore we do not have to fill in a new module version ID in the generated metadata
            // stream.
            Debug.Assert(this.module.PersistentIdentifier != default(Guid));

            uint moduleVersionIdOffsetInMetadataStream;
            uint entryPointToken;

            SerializeMetadataAndIL(
                pdbWriterOpt,
                metadataWriter,
                ilWriter,
                mappedFieldDataWriter,
                managedResourceDataWriter,
                calculateMethodBodyStreamRva: _ => 0,
                calculateMappedFieldDataStreamRva: _ => 0,
                moduleVersionIdOffsetInMetadataStream: out moduleVersionIdOffsetInMetadataStream,
                metadataSizes: out metadataSizes,
                entryPointToken: out entryPointToken);

            ilBuffer.WriteTo(ilStream);
            metadataBuffer.WriteTo(metadataStream);

            Debug.Assert(entryPointToken == 0);
            Debug.Assert(mappedFieldDataBuffer.Length == 0);
            Debug.Assert(managedResourceDataBuffer.Length == 0);
        }

        public void SerializeMetadataAndIL(
            PdbWriter pdbWriterOpt,
            BinaryWriter metadataWriter,
            BinaryWriter ilWriter,
            BinaryWriter mappedFieldDataWriter,
            BinaryWriter managedResourceDataWriter,
            Func<MetadataSizes, int> calculateMethodBodyStreamRva,
            Func<MetadataSizes, int> calculateMappedFieldDataStreamRva,
            out uint moduleVersionIdOffsetInMetadataStream,
            out uint entryPointToken,
            out MetadataSizes metadataSizes)
        {
            // Extract information from object model into tables, indices and streams
            CreateIndices();

            uint[] methodBodyRvas = SerializeMethodBodies(ilWriter, pdbWriterOpt);

            cancellationToken.ThrowIfCancellationRequested();

            // method body serialization adds Stand Alone Signatures
            this.tableIndicesAreComplete = true;

            ReportReferencesToAddedSymbols();

            PopulateTables(methodBodyRvas, mappedFieldDataWriter, managedResourceDataWriter);

            IMethodReference entryPoint = this.module.EntryPoint;
            if (IsFullMetadata && entryPoint?.GetResolvedMethod(Context) != null)
            {
                entryPointToken = GetMethodToken(entryPoint);
            }
            else
            {
                entryPointToken = 0;
            }

            // Do this as soon as table rows are done and before we need final size of string table
            SerializeStringHeap();

            this.streamsAreComplete = true;
            var rowCounts = CalculateRowCounts();

            metadataSizes = new MetadataSizes(
                rowCounts,
                CalculateHeapSizes(),
                ilStreamSize: (int)ilWriter.BaseStream.Length,
                mappedFieldDataSize: (int)mappedFieldDataWriter.BaseStream.Length,
                resourceDataSize: (int)managedResourceDataWriter.BaseStream.Length,
                isMinimalDelta: IsMinimalDelta);

            // Align heaps
            this.OnBeforeHeapsAligned();
            this.stringWriter.Align(4);
            this.userStringWriter.Align(4);
            this.guidWriter.Align(4);
            this.blobWriter.Align(4);

            int methodBodyStreamRva = calculateMethodBodyStreamRva(metadataSizes);
            int mappedFieldDataStreamRva = calculateMappedFieldDataStreamRva(metadataSizes);

            SerializeMetadata(metadataWriter, metadataSizes, methodBodyStreamRva, mappedFieldDataStreamRva, out moduleVersionIdOffsetInMetadataStream);
        }

        private void SerializeMetadata(BinaryWriter metadataWriter, MetadataSizes metadataSizes, int methodBodyStreamRva, int mappedFieldDataStreamRva, out uint moduleVersionIdOffset)
        {
            uint metadataStartOffset = metadataWriter.BaseStream.Position;

            // Leave space for the metadata header. We need to fill in the sizes of all tables and heaps.
            // It's easier to write it at the end then to precalculate the sizes.
            metadataWriter.BaseStream.Position = metadataStartOffset + (uint)metadataSizes.MetadataHeaderSize;
            this.SerializeMetadataTables(metadataWriter, metadataSizes, methodBodyStreamRva, mappedFieldDataStreamRva);
            
            this.stringWriter.BaseStream.WriteTo(metadataWriter.BaseStream);
            this.userStringWriter.BaseStream.WriteTo(metadataWriter.BaseStream);

            uint guidHeapStartOffset = metadataWriter.BaseStream.Position;
            moduleVersionIdOffset = GetModuleVersionGuidOffsetInMetadataStream(guidHeapStartOffset);
            
            this.guidWriter.BaseStream.WriteTo(metadataWriter.BaseStream);
            this.blobWriter.BaseStream.WriteTo(metadataWriter.BaseStream);

            uint metadataSize = metadataWriter.BaseStream.Position;

            // write header at the start of the metadata stream:
            metadataWriter.BaseStream.Position = 0;
            this.SerializeMetadataHeader(metadataWriter, metadataSizes);

            metadataWriter.BaseStream.Position = metadataSize;
        }

        private uint GetModuleVersionGuidOffsetInMetadataStream(uint guidHeapOffsetInMetadataStream)
        {
            // index of module version ID in the guidWriter stream
            uint moduleVersionIdIndex = this.moduleRow.ModuleVersionId;

            // offset into the guidWriter stream of the module version ID
            uint moduleVersionOffsetInGuidTable = (moduleVersionIdIndex - 1) << 4;

            return guidHeapOffsetInMetadataStream + moduleVersionOffsetInGuidTable;
        }

        private void SerializeMetadataTables(BinaryWriter writer, MetadataSizes metadataSizes, int methodBodyStreamRva, int mappedFieldDataStreamRva)
        {
            uint startPosition = writer.BaseStream.Position;

            this.SerializeTablesHeader(writer, metadataSizes);
            this.SerializeModuleTable(writer, metadataSizes);
            this.SerializeTypeRefTable(writer, metadataSizes);
            this.SerializeTypeDefTable(writer, metadataSizes);
            this.SerializeFieldTable(writer, metadataSizes);
            this.SerializeMethodTable(writer, metadataSizes, methodBodyStreamRva);
            this.SerializeParamTable(writer, metadataSizes);
            this.SerializeInterfaceImplTable(writer, metadataSizes);
            this.SerializeMemberRefTable(writer, metadataSizes);
            this.SerializeConstantTable(writer, metadataSizes);
            this.SerializeCustomAttributeTable(writer, metadataSizes);
            this.SerializeFieldMarshalTable(writer, metadataSizes);
            this.SerializeDeclSecurityTable(writer, metadataSizes);
            this.SerializeClassLayoutTable(writer, metadataSizes);
            this.SerializeFieldLayoutTable(writer, metadataSizes);
            this.SerializeStandAloneSigTable(writer, metadataSizes);
            this.SerializeEventMapTable(writer, metadataSizes);
            this.SerializeEventTable(writer, metadataSizes);
            this.SerializePropertyMapTable(writer, metadataSizes);
            this.SerializePropertyTable(writer, metadataSizes);
            this.SerializeMethodSemanticsTable(writer, metadataSizes);
            this.SerializeMethodImplTable(writer, metadataSizes);
            this.SerializeModuleRefTable(writer, metadataSizes);
            this.SerializeTypeSpecTable(writer, metadataSizes);
            this.SerializeImplMapTable(writer, metadataSizes);
            this.SerializeFieldRvaTable(writer, metadataSizes, mappedFieldDataStreamRva);
            this.SerializeEncLogTable(writer);
            this.SerializeEncMapTable(writer);
            this.SerializeAssemblyTable(writer, metadataSizes);
            this.SerializeAssemblyRefTable(writer, metadataSizes);
            this.SerializeFileTable(writer, metadataSizes);
            this.SerializeExportedTypeTable(writer, metadataSizes);
            this.SerializeManifestResourceTable(writer, metadataSizes);
            this.SerializeNestedClassTable(writer, metadataSizes);
            this.SerializeGenericParamTable(writer, metadataSizes);
            this.SerializeMethodSpecTable(writer, metadataSizes);
            this.SerializeGenericParamConstraintTable(writer, metadataSizes);
            writer.WriteByte(0);
            writer.Align(4);

            uint endPosition = writer.BaseStream.Position;
            Debug.Assert(metadataSizes.MetadataTableStreamSize == endPosition - startPosition);
        }

        private void PopulateTables(uint[] methodBodyRvas, BinaryWriter mappedFieldDataWriter, BinaryWriter resourceWriter)
        {
            this.PopulateAssemblyRefTableRows();
            this.PopulateAssemblyTableRows();
            this.PopulateClassLayoutTableRows();
            this.PopulateConstantTableRows();
            this.PopulateDeclSecurityTableRows();
            this.PopulateEventMapTableRows();
            this.PopulateEventTableRows();
            this.PopulateExportedTypeTableRows();
            this.PopulateFieldLayoutTableRows();
            this.PopulateFieldMarshalTableRows();
            this.PopulateFieldRvaTableRows(mappedFieldDataWriter);
            this.PopulateFieldTableRows();
            this.PopulateFileTableRows();
            this.PopulateGenericParamTableRows();
            this.PopulateGenericParamConstraintTableRows();
            this.PopulateImplMapTableRows();
            this.PopulateInterfaceImplTableRows();
            this.PopulateManifestResourceTableRows(resourceWriter);
            this.PopulateMemberRefTableRows();
            this.PopulateMethodImplTableRows();
            this.PopulateMethodTableRows(methodBodyRvas);
            this.PopulateMethodSemanticsTableRows();
            this.PopulateMethodSpecTableRows();
            this.PopulateModuleRefTableRows();
            this.PopulateModuleTableRows();
            this.PopulateNestedClassTableRows();
            this.PopulateParamTableRows();
            this.PopulatePropertyMapTableRows();
            this.PopulatePropertyTableRows();
            this.PopulateTypeDefTableRows();
            this.PopulateTypeRefTableRows();
            this.PopulateTypeSpecTableRows();

            // This table is populated after the others because it depends on the order of the entries of the generic parameter table.
            this.PopulateCustomAttributeTableRows();

            ImmutableArray<int> rowCounts = CalculateRowCounts();
            Debug.Assert(rowCounts[(int)TableIndex.EncLog] == 0 && rowCounts[(int)TableIndex.EncMap] == 0);

            this.PopulateEncLogTableRows(this.encLogTable, rowCounts);
            this.PopulateEncMapTableRows(this.encMapTable, rowCounts);
        }

        private struct AssemblyRefTableRow
        {
            public Version Version;
            public uint PublicKeyToken;
            public StringIdx Name;
            public StringIdx Culture;
            public AssemblyContentType ContentType;
            public bool IsRetargetable;
        }

        private void PopulateAssemblyRefTableRows()
        {
            var assemblyRefs = this.GetAssemblyRefs();
            this.assemblyRefTable.Capacity = assemblyRefs.Count;

            foreach (var assemblyRef in assemblyRefs)
            {
                AssemblyRefTableRow r = new AssemblyRefTableRow();
                r.Version = assemblyRef.Version;
                r.PublicKeyToken = this.GetBlobIndex(assemblyRef.PublicKeyToken);

                Debug.Assert(!string.IsNullOrEmpty(assemblyRef.Name));
                r.Name = this.GetStringIndexForPathAndCheckLength(assemblyRef.Name, assemblyRef);

                r.Culture = this.GetStringIndex(assemblyRef.Culture);

                r.IsRetargetable = assemblyRef.IsRetargetable;
                r.ContentType = assemblyRef.ContentType;
                this.assemblyRefTable.Add(r);
            }
        }

        /// <summary>
        /// Compares quality of assembly references to achieve unique rows in AssemblyRef table.
        /// Metadata spec: "The AssemblyRef table shall contain no duplicates (where duplicate rows are deemd to 
        /// be those having the same MajorVersion, MinorVersion, BuildNumber, RevisionNumber, PublicKeyOrToken, 
        /// Name, and Culture)".
        /// </summary>
        protected sealed class AssemblyReferenceComparer : IEqualityComparer<IAssemblyReference>
        {
            internal static readonly AssemblyReferenceComparer Instance = new AssemblyReferenceComparer();

            public bool Equals(IAssemblyReference x, IAssemblyReference y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                return
                    x.Version.Equals(y.Version) &&
                    ByteSequenceComparer.Instance.Equals(x.PublicKeyToken, y.PublicKeyToken) &&
                    x.Name == y.Name &&
                    x.Culture == y.Culture;
            }

            public int GetHashCode(IAssemblyReference reference)
            {
                return Hash.Combine(reference.Version,
                       Hash.Combine(ByteSequenceComparer.Instance.GetHashCode(reference.PublicKeyToken),
                       Hash.Combine(reference.Name.GetHashCode(),
                       Hash.Combine(reference.Culture, 0))));
            }
        }

        private readonly List<AssemblyRefTableRow> assemblyRefTable = new List<AssemblyRefTableRow>();

        private void PopulateAssemblyTableRows()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return;
            }

            this.assemblyKey = this.GetBlobIndex(assembly.PublicKey);
            this.assemblyName = this.GetStringIndexForPathAndCheckLength(assembly.Name, assembly);
            this.assemblyCulture = this.GetStringIndex(assembly.Culture);
        }

        private uint assemblyKey;
        private StringIdx assemblyName;
        private StringIdx assemblyCulture;

        private void PopulateClassLayoutTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                if (typeDef.Alignment == 0 && typeDef.SizeOf == 0)
                {
                    continue;
                }

                uint typeDefIndex = this.GetTypeDefIndex(typeDef);
                ClassLayoutRow r = new ClassLayoutRow();
                r.PackingSize = typeDef.Alignment;
                r.ClassSize = typeDef.SizeOf;
                r.Parent = typeDefIndex;
                this.classLayoutTable.Add(r);
            }
        }

        private struct ClassLayoutRow { public ushort PackingSize; public uint ClassSize; public uint Parent; }

        private readonly List<ClassLayoutRow> classLayoutTable = new List<ClassLayoutRow>();

        private void PopulateConstantTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                var constant = fieldDef.GetCompileTimeValue(Context);
                if (constant == null)
                {
                    continue;
                }

                uint fieldDefIndex = this.GetFieldDefIndex(fieldDef);
                this.constantTable.Add(CreateConstantRow(constant.Value, parent: fieldDefIndex << 2));
            }

            int sizeWithOnlyFields = this.constantTable.Count;
            foreach (IParameterDefinition parDef in this.GetParameterDefs())
            {
                var defaultValue = parDef.GetDefaultValue(Context);
                if (defaultValue == null)
                {
                    continue;
                }

                uint parameterDefIndex = this.GetParameterDefIndex(parDef);
                this.constantTable.Add(CreateConstantRow(defaultValue.Value, parent: (parameterDefIndex << 2) | 1));
            }

            foreach (IPropertyDefinition propDef in this.GetPropertyDefs())
            {
                if (!propDef.HasDefaultValue)
                {
                    continue;
                }

                uint propertyDefIndex = this.GetPropertyDefIndex(propDef);
                this.constantTable.Add(CreateConstantRow(propDef.DefaultValue.Value, parent: (propertyDefIndex << 2) | 2));
            }

            if (sizeWithOnlyFields > 0 && sizeWithOnlyFields < this.constantTable.Count)
            {
                this.constantTable.Sort(new ConstantRowComparer());
            }
        }

        private class ConstantRowComparer : Comparer<ConstantRow>
        {
            public override int Compare(ConstantRow x, ConstantRow y)
            {
                return ((int)x.Parent) - (int)y.Parent;
            }
        }

        private struct ConstantRow { public byte Type; public uint Parent; public uint Value; }

        private ConstantRow CreateConstantRow(object value, uint parent)
        {
            return new ConstantRow
            {
                Type = (byte)GetConstantTypeCode(value),
                Parent = parent,
                Value = this.GetConstantBlobIndex(value)
            };
        }

        private readonly List<ConstantRow> constantTable = new List<ConstantRow>();

        private void PopulateCustomAttributeTableRows()
        {
            if (this.IsFullMetadata)
            {
                this.AddAssemblyAttributesToTable();
            }

            this.AddCustomAttributesToTable(this.GetMethodDefs(), 0, this.GetMethodDefIndex);
            this.AddCustomAttributesToTable(this.GetFieldDefs(), 1, this.GetFieldDefIndex);

            // this.AddCustomAttributesToTable(this.typeRefList, 2);
            this.AddCustomAttributesToTable(this.GetTypeDefs(), 3, this.GetTypeDefIndex);
            this.AddCustomAttributesToTable(this.GetParameterDefs(), 4, this.GetParameterDefIndex);

            // TODO: attributes on interface implementation entries 5
            // TODO: attributes on member reference entries 6
            if (this.IsFullMetadata)
            {
                this.AddModuleAttributesToTable(this.module, 7);
            }

            // TODO: declarative security entries 8
            this.AddCustomAttributesToTable(this.GetPropertyDefs(), 9, this.GetPropertyDefIndex);
            this.AddCustomAttributesToTable(this.GetEventDefs(), 10, this.GetEventDefIndex);

            // TODO: standalone signature entries 11
            if (this.IsFullMetadata)
            {
                this.AddCustomAttributesToTable(this.module.ModuleReferences, 12);
            }

            // TODO: type spec entries 13
            // this.AddCustomAttributesToTable(this.module.AssemblyReferences, 15);
            // TODO: this.AddCustomAttributesToTable(assembly.Files, 16);
            // TODO: exported types 17
            // TODO: this.AddCustomAttributesToTable(assembly.Resources, 18);

            // The indices of this.GetGenericParameters() do not correspond to the table indices because the
            // the table may be sorted after the list has been constructed.
            // Note that in all other cases, tables that are sorted are sorted in an order that depends
            // only on list indices. The generic parameter table is the sole exception.
            List<IGenericParameter> sortedGenericParameterList = new List<IGenericParameter>();
            foreach (GenericParamRow genericParamRow in this.genericParamTable)
            {
                sortedGenericParameterList.Add(genericParamRow.GenericParameter);
            }

            this.AddCustomAttributesToTable(sortedGenericParameterList, 19);

            this.customAttributeTable.Sort(new CustomAttributeRowComparer());
        }

        private void AddAssemblyAttributesToTable()
        {
            bool writingNetModule = (null == this.module.AsAssembly);
            if (writingNetModule)
            {
                // When writing netmodules, assembly security attributes are not emitted by PopulateDeclSecurityTableRows().
                // Instead, here we make sure they are emitted as regular attributes, attached off the appropriate placeholder
                // System.Runtime.CompilerServices.AssemblyAttributesGoHere* type refs.  This is the contract for publishing
                // assembly attributes in netmodules so they may be migrated to containing/referencing multi-module assemblies,
                // at multi-module assembly build time.
                AddAssemblyAttributesToTable(
                    this.module.AssemblySecurityAttributes.Select(sa => sa.Attribute),
                    writingNetModule,   // needsDummyParent
                    true);              // isSecurity
            }

            AddAssemblyAttributesToTable(
                this.module.AssemblyAttributes,
                writingNetModule,   // needsDummyParent
                false);             // IsSecurity
        }

        private void AddAssemblyAttributesToTable(IEnumerable<ICustomAttribute> assemblyAttributes, bool needsDummyParent, bool isSecurity)
        {
            Debug.Assert(this.IsFullMetadata); // parentToken is not relative
            uint parentToken = (1 << 5) | 14;
            foreach (ICustomAttribute customAttribute in assemblyAttributes)
            {
                if (needsDummyParent)
                {
                    // When writing netmodules, assembly attributes are attached off the appropriate placeholder
                    // System.Runtime.CompilerServices.AssemblyAttributesGoHere* type refs.  This is the contract for publishing
                    // assembly attributes in netmodules so they may be migrated to containing/referencing multi-module assemblies,
                    // at multi-module assembly build time.
                    parentToken = GetDummyAssemblyAttributeParent(isSecurity, customAttribute.AllowMultiple);
                }
                AddCustomAttributeToTable(parentToken, customAttribute);
            }
        }

        private uint GetDummyAssemblyAttributeParent(bool isSecurity, bool allowMultiple)
        {
            // Lazily get or create placeholder assembly attribute parent type ref for the given combination of
            // whether isSecurity and allowMultiple.  Convert type ref row id to corresponding attribute parent tag.
            // Note that according to the defacto contract, although the placeholder type refs have CorLibrary as their
            // resolution scope, the types backing the placeholder type refs need not actually exist.
            int iS = isSecurity ? 1 : 0;
            int iM = allowMultiple ? 1 : 0;
            if (dummyAssemblyAttributeParent[iS, iM] == 0)
            {
                TypeRefRow r = new TypeRefRow();
                r.ResolutionScope = this.GetResolutionScopeCodedIndex(this.module.GetCorLibrary(Context));
                r.Name = this.GetStringIndex(dummyAssemblyAttributeParentName + dummyAssemblyAttributeParentQualifier[iS, iM]);
                r.Namespace = this.GetStringIndex(dummyAssemblyAttributeParentNamespace);
                this.typeRefTable.Add(r);
                dummyAssemblyAttributeParent[iS, iM] = ((uint)this.typeRefTable.Count << 5) | 2;
            }
            return dummyAssemblyAttributeParent[iS, iM];
        }

        private void AddModuleAttributesToTable(IModule module, uint tag)
        {
            Debug.Assert(this.IsFullMetadata); // parentToken is not relative
            uint parentToken = (1 << 5) | tag;
            foreach (ICustomAttribute customAttribute in module.ModuleAttributes)
            {
                AddCustomAttributeToTable(parentToken, customAttribute);
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, uint tag)
            where T : IReference
        {
            uint parentIndex = 0;
            foreach (var parent in parentList)
            {
                parentIndex++;
                uint parentToken = (parentIndex << 5) | tag;
                foreach (ICustomAttribute customAttribute in parent.GetAttributes(Context))
                {
                    AddCustomAttributeToTable(parentToken, customAttribute);
                }
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, uint tag, Func<T, uint> getDefIndex)
            where T : IReference
        {
            foreach (var parent in parentList)
            {
                uint parentIndex = getDefIndex(parent);
                uint parentToken = (parentIndex << 5) | tag;
                foreach (ICustomAttribute customAttribute in parent.GetAttributes(Context))
                {
                    AddCustomAttributeToTable(parentToken, customAttribute);
                }
            }
        }

        private void AddCustomAttributeToTable(uint parentToken, ICustomAttribute customAttribute)
        {
            CustomAttributeRow r = new CustomAttributeRow();
            r.Parent = parentToken;
            var ctor = customAttribute.Constructor(Context);
            r.Type = this.GetCustomAttributeTypeCodedIndex(ctor);
            r.Value = this.GetCustomAttributeSignatureIndex(customAttribute);
            r.OriginalPosition = this.customAttributeTable.Count;
            this.customAttributeTable.Add(r);
        }

        private class CustomAttributeRowComparer : Comparer<CustomAttributeRow>
        {
            public override int Compare(CustomAttributeRow x, CustomAttributeRow y)
            {
                int result = ((int)x.Parent) - (int)y.Parent;
                if (result == 0)
                {
                    result = x.OriginalPosition - y.OriginalPosition;
                }

                return result;
            }
        }

        private struct CustomAttributeRow { public uint Parent; public uint Type; public uint Value; public int OriginalPosition; }

        private readonly List<CustomAttributeRow> customAttributeTable = new List<CustomAttributeRow>();

        private void PopulateDeclSecurityTableRows()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly != null)
            {
                this.PopulateDeclSecurityTableRowsFor((1 << 2) | 2, assembly.AssemblySecurityAttributes);
            }

            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                if (!typeDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                uint typeDefIndex = this.GetTypeDefIndex(typeDef);
                this.PopulateDeclSecurityTableRowsFor(typeDefIndex << 2, typeDef.SecurityAttributes);
            }

            foreach (IMethodDefinition methodDef in this.GetMethodDefs())
            {
                if (!methodDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                uint methodDefIndex = this.GetMethodDefIndex(methodDef);
                this.PopulateDeclSecurityTableRowsFor((methodDefIndex << 2) | 1, methodDef.SecurityAttributes);
            }

            this.declSecurityTable.Sort(new DeclSecurityRowComparer());
        }

        private void PopulateDeclSecurityTableRowsFor(uint parent, IEnumerable<SecurityAttribute> attributes)
        {
            OrderPreservingMultiDictionary<SecurityAction, ICustomAttribute> groupedSecurityAttributes = null;

            foreach (SecurityAttribute securityAttribute in attributes)
            {
                groupedSecurityAttributes = groupedSecurityAttributes ?? OrderPreservingMultiDictionary<SecurityAction, ICustomAttribute>.GetInstance();
                groupedSecurityAttributes.Add(securityAttribute.Action, securityAttribute.Attribute);
            }

            if (groupedSecurityAttributes == null)
            {
                return;
            }

            DeclSecurityRow r = new DeclSecurityRow();
            r.Parent = parent;

            foreach (SecurityAction securityAction in groupedSecurityAttributes.Keys)
            {
                r.Action = (ushort)securityAction;
                r.PermissionSet = this.GetPermissionSetIndex(groupedSecurityAttributes[securityAction]);
                r.OriginalIndex = this.declSecurityTable.Count;
                this.declSecurityTable.Add(r);
            }

            groupedSecurityAttributes.Free();
        }

        private class DeclSecurityRowComparer : Comparer<DeclSecurityRow>
        {
            public override int Compare(DeclSecurityRow x, DeclSecurityRow y)
            {
                int result = (int)x.Parent - (int)y.Parent;
                if (result == 0)
                {
                    result = x.OriginalIndex - y.OriginalIndex;
                }

                return result;
            }
        }

        private struct DeclSecurityRow { public ushort Action; public uint Parent; public uint PermissionSet; public int OriginalIndex; }

        private readonly List<DeclSecurityRow> declSecurityTable = new List<DeclSecurityRow>();

        protected struct EncLogRow { public uint Token; public EncFuncCode FuncCode; }

        private readonly List<EncLogRow> encLogTable = new List<EncLogRow>();
        
        protected struct EncMapRow { public uint Token; }

        private readonly List<EncMapRow> encMapTable = new List<EncMapRow>();

        private void PopulateEventMapTableRows()
        {
            this.PopulateEventMapTableRows(this.eventMapTable);
        }

        protected struct EventMapRow { public uint Parent; public uint EventList; }

        private readonly List<EventMapRow> eventMapTable = new List<EventMapRow>();

        private void PopulateEventTableRows()
        {
            var eventDefs = this.GetEventDefs();
            this.eventTable.Capacity = eventDefs.Count;

            foreach (IEventDefinition eventDef in eventDefs)
            {
                EventRow r = new EventRow();
                r.EventFlags = GetEventFlags(eventDef);
                r.Name = this.GetStringIndexForNameAndCheckLength(eventDef.Name, eventDef);
                r.EventType = this.GetTypeDefOrRefCodedIndex(eventDef.GetType(Context), true);
                this.eventTable.Add(r);
            }
        }

        private struct EventRow { public ushort EventFlags; public StringIdx Name; public uint EventType; }

        private readonly List<EventRow> eventTable = new List<EventRow>();

        private void PopulateExportedTypeTableRows()
        {
            if (this.IsFullMetadata)
            {
                this.exportedTypeTable.Capacity = this.NumberOfTypeDefsEstimate;

                foreach (ITypeExport typeExport in this.module.GetExportedTypes(Context))
                {
                    ITypeReference exportedType = typeExport.ExportedType;
                    INestedTypeReference nestedRef = null;
                    INamespaceTypeReference namespaceTypeRef = null;
                    ExportedTypeRow r = new ExportedTypeRow();
                    r.TypeDefId = (uint)MetadataTokens.GetToken(exportedType.TypeDef);
                    if ((namespaceTypeRef = exportedType.AsNamespaceTypeReference) != null)
                    {
                        r.Flags = TypeFlags.PublicAccess;
                        string mangledTypeName = GetMangledName(namespaceTypeRef);
                        r.TypeName = this.GetStringIndexForNameAndCheckLength(mangledTypeName, namespaceTypeRef);
                        r.TypeNamespace = this.GetStringIndexForNamespaceAndCheckLength(namespaceTypeRef, mangledTypeName);
                        r.Implementation = this.GetImplementationCodedIndex(namespaceTypeRef);
                        if ((r.Implementation & 1) == 1)
                        {
                            r.Flags = TypeFlags.PrivateAccess | TypeFlags.ForwarderImplementation;
                            r.TypeDefId = 0; // Must be cleared for type forwarders.
                        }
                    }
                    else if ((nestedRef = exportedType.AsNestedTypeReference) != null)
                    {
                        r.Flags = TypeFlags.NestedPublicAccess;
                        r.TypeName = this.GetStringIndexForNameAndCheckLength(GetMangledName(nestedRef), nestedRef);
                        r.TypeNamespace = StringIdx.Empty;

                        var containingType = nestedRef.GetContainingType(Context);
                        uint ci = this.GetExportedTypeIndex(containingType);
                        r.Implementation = (ci << 2) | 2;

                        var parentFlags = this.exportedTypeTable[((int)ci) - 1].Flags;
                        if (parentFlags == TypeFlags.PrivateAccess)
                        {
                            r.Flags = TypeFlags.PrivateAccess;
                        }

                        ITypeReference topLevelType = containingType;
                        INestedTypeReference tmp;
                        while ((tmp = topLevelType.AsNestedTypeReference) != null)
                        {
                            topLevelType = tmp.GetContainingType(Context);
                        }

                        var topLevelFlags = this.exportedTypeTable[(int)this.GetExportedTypeIndex(topLevelType) - 1].Flags;
                        if ((topLevelFlags & TypeFlags.ForwarderImplementation) != 0)
                        {
                            r.Flags = TypeFlags.PrivateAccess;
                            r.TypeDefId = 0; // Must be cleared for type forwarders and types they contain.
                        }
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(exportedType);
                    }

                    this.exportedTypeTable.Add(r);
                }
            }
        }

        private struct ExportedTypeRow { public TypeFlags Flags; public uint TypeDefId; public StringIdx TypeName; public StringIdx TypeNamespace; public uint Implementation; }

        private readonly List<ExportedTypeRow> exportedTypeTable = new List<ExportedTypeRow>();

        private void PopulateFieldLayoutTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (fieldDef.ContainingTypeDefinition.Layout != LayoutKind.Explicit || fieldDef.IsStatic)
                {
                    continue;
                }

                uint fieldDefIndex = this.GetFieldDefIndex(fieldDef);
                FieldLayoutRow r = new FieldLayoutRow();
                r.Offset = fieldDef.Offset;
                r.Field = fieldDefIndex;
                this.fieldLayoutTable.Add(r);
            }
        }

        private struct FieldLayoutRow { public uint Offset; public uint Field; }

        private readonly List<FieldLayoutRow> fieldLayoutTable = new List<FieldLayoutRow>();

        private void PopulateFieldMarshalTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (!fieldDef.IsMarshalledExplicitly)
                {
                    continue;
                }

                FieldMarshalRow r = new FieldMarshalRow();

                var marshallingInformation = fieldDef.MarshallingInformation;

                if (marshallingInformation != null)
                {
                    r.NativeType = this.GetMarshallingDescriptorIndex(marshallingInformation);
                }
                else
                {
                    r.NativeType = this.GetMarshallingDescriptorIndex(fieldDef.MarshallingDescriptor);
                }

                uint fieldDefIndex = this.GetFieldDefIndex(fieldDef);
                r.Parent = fieldDefIndex << 1;
                this.fieldMarshalTable.Add(r);
            }

            int sizeWithOnlyFields = this.fieldMarshalTable.Count;
            foreach (IParameterDefinition parDef in this.GetParameterDefs())
            {
                if (!parDef.IsMarshalledExplicitly)
                {
                    continue;
                }

                FieldMarshalRow r = new FieldMarshalRow();

                var marshallingInformation = parDef.MarshallingInformation;

                if (marshallingInformation != null)
                {
                    r.NativeType = this.GetMarshallingDescriptorIndex(marshallingInformation);
                }
                else
                {
                    r.NativeType = this.GetMarshallingDescriptorIndex(parDef.MarshallingDescriptor);
                }

                uint parameterDefIndex = this.GetParameterDefIndex(parDef);
                r.Parent = (parameterDefIndex << 1) | 1;
                this.fieldMarshalTable.Add(r);
            }

            if (sizeWithOnlyFields > 0 && sizeWithOnlyFields < this.fieldMarshalTable.Count)
            {
                this.fieldMarshalTable.Sort(new FieldMarshalRowComparer());
            }
        }

        private class FieldMarshalRowComparer : Comparer<FieldMarshalRow>
        {
            public override int Compare(FieldMarshalRow x, FieldMarshalRow y)
            {
                return ((int)x.Parent) - (int)y.Parent;
            }
        }

        private struct FieldMarshalRow { public uint Parent; public uint NativeType; }

        private readonly List<FieldMarshalRow> fieldMarshalTable = new List<FieldMarshalRow>();

        private void PopulateFieldRvaTableRows(BinaryWriter mappedFieldDataWriter)
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (fieldDef.MappedData.IsDefault)
                {
                    continue;
                }

                uint fieldIndex = this.GetFieldDefIndex(fieldDef);
                FieldRvaRow r = new FieldRvaRow();

                r.Offset = mappedFieldDataWriter.BaseStream.Position;
                mappedFieldDataWriter.WriteBytes(fieldDef.MappedData);
                mappedFieldDataWriter.Align(MappedFieldDataAlignment);

                r.Field = fieldIndex;
                this.fieldRvaTable.Add(r);
            }
        }

        private struct FieldRvaRow { public uint Offset; public uint Field; }

        private readonly List<FieldRvaRow> fieldRvaTable = new List<FieldRvaRow>();

        private void PopulateFieldTableRows()
        {
            var fieldDefs = this.GetFieldDefs();
            this.fieldDefTable.Capacity = fieldDefs.Count;

            foreach (IFieldDefinition fieldDef in fieldDefs)
            {
                FieldDefRow r = new FieldDefRow();
                r.Flags = GetFieldFlags(fieldDef);

                if (fieldDef.IsContextualNamedEntity)
                {
                    ((IContextualNamedEntity)fieldDef).AssociateWithMetadataWriter(this);
                }

                r.Name = this.GetStringIndexForNameAndCheckLength(fieldDef.Name, fieldDef);
                r.Signature = this.GetFieldSignatureIndex(fieldDef);
                this.fieldDefTable.Add(r);
            }
        }

        private struct FieldDefRow { public ushort Flags; public StringIdx Name; public uint Signature; }

        private readonly List<FieldDefRow> fieldDefTable = new List<FieldDefRow>();

        private void PopulateFileTableRows()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return;
            }

            var hashAlgorithm = assembly.HashAlgorithm;
            this.fileTable.Capacity = fileRefList.Count;

            foreach (IFileReference fileReference in fileRefList)
            {
                FileTableRow r = new FileTableRow();
                r.Flags = fileReference.HasMetadata ? 0u : 1u;
                r.FileName = this.GetStringIndexForPathAndCheckLength(fileReference.FileName);
                r.HashValue = this.GetBlobIndex(fileReference.GetHashValue(hashAlgorithm));
                this.fileTable.Add(r);
            }
        }

        private struct FileTableRow { public uint Flags; public StringIdx FileName; public uint HashValue; }

        private readonly List<FileTableRow> fileTable = new List<FileTableRow>();

        private void PopulateGenericParamConstraintTableRows()
        {
            uint genericParamIndex = 0;
            foreach (GenericParamRow genericParameterRow in this.genericParamTable)
            {
                genericParamIndex++;
                GenericParamConstraintRow r = new GenericParamConstraintRow();
                r.Owner = genericParamIndex;
                foreach (ITypeReference constraint in genericParameterRow.GenericParameter.GetConstraints(Context))
                {
                    r.Constraint = this.GetTypeDefOrRefCodedIndex(constraint, true);
                    this.genericParamConstraintTable.Add(r);
                }
            }
        }

        private struct GenericParamConstraintRow { public uint Owner; public uint Constraint; }

        private readonly List<GenericParamConstraintRow> genericParamConstraintTable = new List<GenericParamConstraintRow>();

        private void PopulateGenericParamTableRows()
        {
            var genericParameters = this.GetGenericParameters();
            this.genericParamTable.Capacity = genericParameters.Count;

            foreach (IGenericParameter genPar in genericParameters)
            {
                GenericParamRow r = new GenericParamRow();
                r.Number = genPar.Index;
                r.Flags = GetGenericParamFlags(genPar);
                r.Owner = this.GetTypeOrMethodDefCodedIndex(genPar);

                // CONSIDER: The CLI spec doesn't mention a restriction on the Name column of the GenericParam table,
                // but they go in the same string heap as all the other declaration names, so it stands to reason that
                // they should be restricted in the same way.
                r.Name = this.GetStringIndexForNameAndCheckLength(genPar.Name, genPar);

                r.GenericParameter = genPar;
                this.genericParamTable.Add(r);
            }

            this.genericParamTable.Sort(new GenericParamRowComparer());
        }

        private class GenericParamRowComparer : Comparer<GenericParamRow>
        {
            public override int Compare(GenericParamRow x, GenericParamRow y)
            {
                int result = ((int)x.Owner) - (int)y.Owner;
                if (result != 0)
                {
                    return result;
                }

                return ((int)x.Number) - (int)y.Number;
            }
        }

        private struct GenericParamRow { public ushort Number; public ushort Flags; public uint Owner; public StringIdx Name; public IGenericParameter GenericParameter; }

        private readonly List<GenericParamRow> genericParamTable = new List<GenericParamRow>();

        private void PopulateImplMapTableRows()
        {
            foreach (IMethodDefinition methodDef in this.GetMethodDefs())
            {
                if (!methodDef.IsPlatformInvoke)
                {
                    continue;
                }

                var data = methodDef.PlatformInvokeData;
                uint methodDefIndex = this.GetMethodDefIndex(methodDef);
                ImplMapRow r = new ImplMapRow();
                r.MappingFlags = (ushort)data.Flags;
                r.MemberForwarded = (methodDefIndex << 1) | 1;

                string entryPointName = data.EntryPointName;
                if (entryPointName != null)
                {
                    r.ImportName = this.GetStringIndexForNameAndCheckLength(entryPointName, methodDef);
                }
                else
                {
                    r.ImportName = this.GetStringIndex(methodDef.Name); // Length checked while populating the method def table.
                }

                r.ImportScope = this.GetModuleRefIndex(data.ModuleName);
                this.implMapTable.Add(r);
            }
        }

        private struct ImplMapRow { public ushort MappingFlags; public uint MemberForwarded; public StringIdx ImportName; public uint ImportScope; }

        private readonly List<ImplMapRow> implMapTable = new List<ImplMapRow>();

        private void PopulateInterfaceImplTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                uint typeDefIndex = this.GetTypeDefIndex(typeDef);
                foreach (ITypeReference interfaceRef in typeDef.Interfaces(Context))
                {
                    InterfaceImplRow r = new InterfaceImplRow();
                    r.Class = typeDefIndex;
                    r.Interface = this.GetTypeDefOrRefCodedIndex(interfaceRef, true);
                    this.interfaceImplTable.Add(r);
                }
            }
        }

        private struct InterfaceImplRow { public uint Class; public uint Interface; }

        private readonly List<InterfaceImplRow> interfaceImplTable = new List<InterfaceImplRow>();

        private void PopulateManifestResourceTableRows(BinaryWriter resourceDataWriter)
        {
            foreach (var resource in this.module.GetResources(Context))
            {
                ManifestResourceRow r = new ManifestResourceRow();
                r.Offset = this.GetManagedResourceOffset(resource, resourceDataWriter);
                r.Flags = resource.IsPublic ? 1u : 2u;
                r.Name = this.GetStringIndexForNameAndCheckLength(resource.Name);

                if (resource.ExternalFile != null)
                {
                    IFileReference externalFile = resource.ExternalFile;
                    // Length checked on insertion into the file table.
                    r.Implementation = this.GetFileRefIndex(externalFile) << 2;
                }
                else
                {
                    // This is an embedded resource, we don't support references to resources from referenced assemblies.
                    r.Implementation = 0;
                }

                this.manifestResourceTable.Add(r);
            }

            // the stream should be aligned:
            Debug.Assert((resourceDataWriter.BaseStream.Length % 8) == 0);
        }

        private struct ManifestResourceRow { public uint Offset; public uint Flags; public StringIdx Name; public uint Implementation; }

        private readonly List<ManifestResourceRow> manifestResourceTable = new List<ManifestResourceRow>();

        private void PopulateMemberRefTableRows()
        {
            var memberRefs = this.GetMemberRefs();
            this.memberRefTable.Capacity = memberRefs.Count;

            foreach (ITypeMemberReference memberRef in memberRefs)
            {
                MemberRefRow r = new MemberRefRow();
                r.Class = this.GetMemberRefParentCodedIndex(memberRef);
                r.Name = this.GetStringIndexForNameAndCheckLength(memberRef.Name, memberRef);
                r.Signature = this.GetMemberRefSignatureIndex(memberRef);
                this.memberRefTable.Add(r);
            }
        }

        private struct MemberRefRow { public uint Class; public StringIdx Name; public uint Signature; }

        private readonly List<MemberRefRow> memberRefTable = new List<MemberRefRow>();

        private void PopulateMethodImplTableRows()
        {
            this.methodImplTable.Capacity = this.methodImplList.Count;

            foreach (MethodImplementation methodImplementation in this.methodImplList)
            {
                MethodImplRow r = new MethodImplRow();
                r.Class = this.GetTypeDefIndex(methodImplementation.ContainingType);
                r.MethodBody = this.GetMethodDefOrRefCodedIndex(methodImplementation.ImplementingMethod);
                r.MethodDecl = this.GetMethodDefOrRefCodedIndex(methodImplementation.ImplementedMethod);
                this.methodImplTable.Add(r);
            }
        }

        private struct MethodImplRow { public uint Class; public uint MethodBody; public uint MethodDecl; }

        private readonly List<MethodImplRow> methodImplTable = new List<MethodImplRow>();

        private void PopulateMethodSemanticsTableRows()
        {
            var propertyDefs = this.GetPropertyDefs();
            var eventDefs = this.GetEventDefs();

            //EDMAURER an estimate, not necessarily accurate.
            this.methodSemanticsTable.Capacity = propertyDefs.Count * 2 + eventDefs.Count * 2;

            uint i = 0;
            foreach (IPropertyDefinition propertyDef in this.GetPropertyDefs())
            {
                uint propertyIndex = this.GetPropertyDefIndex(propertyDef);
                MethodSemanticsRow r = new MethodSemanticsRow();
                r.Association = (propertyIndex << 1) | 1;
                foreach (IMethodReference accessorMethod in propertyDef.Accessors)
                {
                    if (accessorMethod == propertyDef.Setter)
                    {
                        r.Semantic = 0x0001;
                    }
                    else if (accessorMethod == propertyDef.Getter)
                    {
                        r.Semantic = 0x0002;
                    }
                    else
                    {
                        r.Semantic = 0x0004;
                    }

                    r.Method = this.GetMethodDefIndex(accessorMethod.GetResolvedMethod(Context));
                    r.OriginalIndex = i++;
                    this.methodSemanticsTable.Add(r);
                }
            }

            int propertiesOnlyTableCount = this.methodSemanticsTable.Count;
            foreach (IEventDefinition eventDef in this.GetEventDefs())
            {
                uint eventIndex = this.GetEventDefIndex(eventDef);
                MethodSemanticsRow r = new MethodSemanticsRow();
                r.Association = eventIndex << 1;
                foreach (IMethodReference accessorMethod in eventDef.Accessors)
                {
                    r.Semantic = 0x0004;
                    if (accessorMethod == eventDef.Adder)
                    {
                        r.Semantic = 0x0008;
                    }
                    else if (accessorMethod == eventDef.Remover)
                    {
                        r.Semantic = 0x0010;
                    }
                    else if (accessorMethod == eventDef.Caller)
                    {
                        r.Semantic = 0x0020;
                    }

                    r.Method = this.GetMethodDefIndex(accessorMethod.GetResolvedMethod(Context));
                    r.OriginalIndex = i++;
                    this.methodSemanticsTable.Add(r);
                }
            }

            if (this.methodSemanticsTable.Count > propertiesOnlyTableCount)
            {
                this.methodSemanticsTable.Sort(new MethodSemanticsRowComparer());
            }
        }

        private class MethodSemanticsRowComparer : Comparer<MethodSemanticsRow>
        {
            public override int Compare(MethodSemanticsRow x, MethodSemanticsRow y)
            {
                int result = ((int)x.Association) - (int)y.Association;
                if (result == 0)
                {
                    result = ((int)x.OriginalIndex) - (int)y.OriginalIndex;
                }

                return result;
            }
        }

        private struct MethodSemanticsRow { public ushort Semantic; public uint Method; public uint Association; public uint OriginalIndex; }

        private readonly List<MethodSemanticsRow> methodSemanticsTable = new List<MethodSemanticsRow>();

        private void PopulateMethodSpecTableRows()
        {
            var methodSpecs = this.GetMethodSpecs();
            this.methodSpecTable.Capacity = methodSpecs.Count;

            foreach (IGenericMethodInstanceReference genericMethodInstanceReference in methodSpecs)
            {
                MethodSpecRow r = new MethodSpecRow();
                r.Method = this.GetMethodDefOrRefCodedIndex(genericMethodInstanceReference.GetGenericMethod(Context));
                r.Instantiation = this.GetGenericMethodInstanceIndex(genericMethodInstanceReference);
                this.methodSpecTable.Add(r);
            }
        }

        private struct MethodSpecRow { public uint Method; public uint Instantiation; }

        private readonly List<MethodSpecRow> methodSpecTable = new List<MethodSpecRow>();

        private void PopulateMethodTableRows(uint[] methodBodyRvas)
        {
            var methodDefs = this.GetMethodDefs();
            this.methodTable = new MethodRow[methodDefs.Count];

            int i = 0;
            foreach (IMethodDefinition methodDef in methodDefs)
            {
                this.methodTable[i] = new MethodRow
                {
                    Rva = methodBodyRvas[i],
                    ImplFlags = (ushort)methodDef.GetImplementationAttributes(Context),
                    Flags = GetMethodFlags(methodDef),
                    Name = this.GetStringIndexForNameAndCheckLength(methodDef.Name, methodDef),
                    Signature = this.GetMethodSignatureIndex(methodDef),
                    ParamList = this.GetParameterDefIndex(methodDef),
                };

                i++;
            }
        }

        private struct MethodRow { public uint Rva; public ushort ImplFlags; public ushort Flags; public StringIdx Name; public uint Signature; public uint ParamList; }

        private MethodRow[] methodTable;

        private void PopulateModuleRefTableRows()
        {
            var moduleRefs = this.GetModuleRefs();
            this.moduleRefTable.Capacity = moduleRefs.Count;

            foreach (string moduleName in moduleRefs)
            {
                ModuleRefRow r = new ModuleRefRow();
                r.Name = this.GetStringIndexForPathAndCheckLength(moduleName);
                this.moduleRefTable.Add(r);
            }
        }

        private struct ModuleRefRow { public StringIdx Name; }

        private readonly List<ModuleRefRow> moduleRefTable = new List<ModuleRefRow>();

        private void PopulateModuleTableRows()
        {
            var r = new ModuleRow();
            r.Generation = this.Generation;
            r.Name = this.GetStringIndexForPathAndCheckLength(this.module.ModuleName);
            r.ModuleVersionId = this.MakeModuleVersionIdGuidIndex();
            r.EncId = this.GetGuidIndex(this.EncId);
            r.EncBaseId = this.GetGuidIndex(this.EncBaseId);
            this.moduleRow = r;
        }

        private struct ModuleRow { public ushort Generation; public StringIdx Name; public uint ModuleVersionId; public uint EncId; public uint EncBaseId; }

        private ModuleRow moduleRow;

        private void PopulateNestedClassTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(Context);
                if (nestedTypeDef == null)
                {
                    continue;
                }

                NestedClassRow r = new NestedClassRow();
                uint typeDefIndex = this.GetTypeDefIndex(typeDef);
                r.NestedClass = typeDefIndex;
                r.EnclosingClass = this.GetTypeDefIndex(nestedTypeDef.ContainingTypeDefinition);
                this.nestedClassTable.Add(r);
            }
        }

        private struct NestedClassRow { public uint NestedClass; public uint EnclosingClass; }

        private readonly List<NestedClassRow> nestedClassTable = new List<NestedClassRow>();

        private void PopulateParamTableRows()
        {
            var parameterDefs = this.GetParameterDefs();
            this.paramTable.Capacity = parameterDefs.Count;

            foreach (IParameterDefinition parDef in parameterDefs)
            {
                ParamRow r = new ParamRow();
                r.Flags = GetParameterFlags(parDef);
                r.Sequence = (ushort)(parDef is ReturnValueParameter ? 0 : parDef.Index + 1);
                r.Name = this.GetStringIndexForNameAndCheckLength(parDef.Name, parDef);
                this.paramTable.Add(r);
            }
        }

        private struct ParamRow { public ushort Flags; public ushort Sequence; public StringIdx Name; }

        private readonly List<ParamRow> paramTable = new List<ParamRow>();

        private void PopulatePropertyMapTableRows()
        {
            this.PopulatePropertyMapTableRows(this.propertyMapTable);
        }

        protected struct PropertyMapRow { public uint Parent; public uint PropertyList; }

        private readonly List<PropertyMapRow> propertyMapTable = new List<PropertyMapRow>();

        private void PopulatePropertyTableRows()
        {
            var propertyDefs = this.GetPropertyDefs();
            this.propertyTable.Capacity = propertyDefs.Count;

            foreach (IPropertyDefinition propertyDef in propertyDefs)
            {
                PropertyRow r = new PropertyRow();
                r.PropFlags = GetPropertyFlags(propertyDef);
                r.Name = this.GetStringIndexForNameAndCheckLength(propertyDef.Name, propertyDef);
                r.Type = this.GetPropertySignatureIndex(propertyDef);
                this.propertyTable.Add(r);
            }
        }

        private struct PropertyRow { public ushort PropFlags; public StringIdx Name; public uint Type; }

        private readonly List<PropertyRow> propertyTable = new List<PropertyRow>();

        private void PopulateTypeDefTableRows()
        {
            var typeDefs = this.GetTypeDefs();
            this.typeDefTable.Capacity = typeDefs.Count;

            foreach (INamedTypeDefinition typeDef in typeDefs)
            {
                TypeDefRow r = new TypeDefRow();
                INamespaceTypeDefinition namespaceType = typeDef.AsNamespaceTypeDefinition(Context);
                r.Flags = GetTypeDefFlags(typeDef);
                string mangledTypeName = GetMangledName(typeDef);
                r.Name = this.GetStringIndexForNameAndCheckLength(mangledTypeName, typeDef);
                r.Namespace = namespaceType == null
                    ? StringIdx.Empty
                    : this.GetStringIndexForNamespaceAndCheckLength(namespaceType, mangledTypeName);
                ITypeReference baseType = typeDef.GetBaseClass(Context);
                r.Extends = (baseType != null) ? this.GetTypeDefOrRefCodedIndex(baseType, true) : 0;

                r.FieldList = this.GetFieldDefIndex(typeDef);
                r.MethodList = this.GetMethodDefIndex(typeDef);

                this.typeDefTable.Add(r);
            }
        }

        private struct TypeDefRow { public uint Flags; public StringIdx Name; public StringIdx Namespace; public uint Extends; public uint FieldList; public uint MethodList; }

        private readonly List<TypeDefRow> typeDefTable = new List<TypeDefRow>();

        private void PopulateTypeRefTableRows()
        {
            var typeRefs = this.GetTypeRefs();
            this.typeRefTable.Capacity = typeRefs.Count;

            foreach (ITypeReference typeRef in typeRefs)
            {
                TypeRefRow r = new TypeRefRow();
                INestedTypeReference nestedTypeRef = typeRef.AsNestedTypeReference;
                if (nestedTypeRef != null)
                {
                    ISpecializedNestedTypeReference sneTypeRef = nestedTypeRef.AsSpecializedNestedTypeReference;
                    if (sneTypeRef != null)
                    {
                        r.ResolutionScope = this.GetResolutionScopeCodedIndex(sneTypeRef.UnspecializedVersion.GetContainingType(Context));
                    }
                    else
                    {
                        r.ResolutionScope = this.GetResolutionScopeCodedIndex(nestedTypeRef.GetContainingType(Context));
                    }

                    r.Name = this.GetStringIndexForNameAndCheckLength(GetMangledName(nestedTypeRef), nestedTypeRef);
                    r.Namespace = StringIdx.Empty;
                }
                else
                {
                    INamespaceTypeReference namespaceTypeRef = typeRef.AsNamespaceTypeReference;
                    if (namespaceTypeRef == null)
                    {
                        throw ExceptionUtilities.UnexpectedValue(typeRef);
                    }

                    r.ResolutionScope = this.GetResolutionScopeCodedIndex(namespaceTypeRef.GetUnit(Context));
                    string mangledTypeName = GetMangledName(namespaceTypeRef);
                    r.Name = this.GetStringIndexForNameAndCheckLength(mangledTypeName, namespaceTypeRef);
                    r.Namespace = this.GetStringIndexForNamespaceAndCheckLength(namespaceTypeRef, mangledTypeName);
                }

                this.typeRefTable.Add(r);
            }
        }

        private struct TypeRefRow { public uint ResolutionScope; public StringIdx Name; public StringIdx Namespace; }

        private readonly List<TypeRefRow> typeRefTable = new List<TypeRefRow>();

        private void PopulateTypeSpecTableRows()
        {
            var typeSpecs = this.GetTypeSpecs();
            this.typeSpecTable.Capacity = typeSpecs.Count;

            foreach (ITypeReference typeSpec in typeSpecs)
            {
                TypeSpecRow r = new TypeSpecRow();
                r.Signature = this.GetTypeSpecSignatureIndex(typeSpec);
                this.typeSpecTable.Add(r);
            }
        }

        private struct TypeSpecRow { public uint Signature; }

        private readonly List<TypeSpecRow> typeSpecTable = new List<TypeSpecRow>();

        private void SerializeTablesHeader(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            uint startPosition = writer.BaseStream.Position;

            HeapSizeFlag heapSizes = 0;
            if (metadataSizes.StringIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.StringHeapLarge;
            }

            if (metadataSizes.GuidIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.GuidHeapLarge;
            }

            if (metadataSizes.BlobIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.BlobHeapLarge;
            }

            if (!this.IsFullMetadata)
            {
                heapSizes |= (HeapSizeFlag.EnCDeltas | HeapSizeFlag.DeletedMarks);
            }

            ulong validTables = 0;
            ulong sortedTables = 0;
            ComputeValidAndSortedMasks(metadataSizes, out validTables, out sortedTables);

            writer.WriteUint(0); // reserved
            writer.WriteByte(this.module.MetadataFormatMajorVersion);
            writer.WriteByte(this.module.MetadataFormatMinorVersion);
            writer.WriteByte((byte)heapSizes);
            writer.WriteByte(1); // reserved
            writer.WriteUlong(validTables);
            writer.WriteUlong(sortedTables);
            SerializeRowCounts(writer, metadataSizes);

            uint endPosition = writer.BaseStream.Position;
            Debug.Assert(metadataSizes.CalculateTableStreamHeaderSize() == endPosition - startPosition);
        }

        private static void ComputeValidAndSortedMasks(MetadataSizes metadataSizes, out ulong validTables, out ulong sortedTables)
        {
            validTables = 0;
            ulong validBit = 1;

            foreach (int rowCount in metadataSizes.RowCounts)
            {
                if (rowCount > 0)
                {
                    validTables |= validBit;
                }

                validBit <<= 1;
            }

            sortedTables = 0x16003301fa00/* & validTables*/;
        }

        private static void SerializeRowCounts(BinaryWriter writer, MetadataSizes tableSizes)
        {
            foreach (int rowCount in tableSizes.RowCounts)
            {
                if (rowCount > 0)
                {
                    writer.WriteInt(rowCount);
                }
            }
        }

        private static void SerializeMetadataConstantValue(object value, BinaryWriter writer)
        {
            if (value == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a 32-bit.
                writer.WriteUint(0);
                return;
            }

            var type = value.GetType();
            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                writer.WriteBool((bool)value);
            }
            else if (type == typeof(int))
            {
                writer.WriteInt((int)value);
            }
            else if (type == typeof(string))
            {
                writer.WriteString((string)value);
            }
            else if (type == typeof(byte))
            {
                writer.WriteByte((byte)value);
            }
            else if (type == typeof(char))
            {
                writer.WriteUshort((char)value);
            }
            else if (type == typeof(double))
            {
                writer.WriteDouble((double)value);
            }
            else if (type == typeof(short))
            {
                writer.WriteShort((short)value);
            }
            else if (type == typeof(long))
            {
                writer.WriteLong((long)value);
            }
            else if (type == typeof(sbyte))
            {
                writer.WriteSbyte((sbyte)value);
            }
            else if (type == typeof(float))
            {
                writer.WriteFloat((float)value);
            }
            else if (type == typeof(ushort))
            {
                writer.WriteUshort((ushort)value);
            }
            else if (type == typeof(uint))
            {
                writer.WriteUint((uint)value);
            }
            else if (type == typeof(ulong))
            {
                writer.WriteUlong((ulong)value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void SerializeModuleTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            writer.WriteUshort(this.moduleRow.Generation);
            SerializeIndex(writer, this.moduleRow.Name, metadataSizes.StringIndexSize);
            SerializeIndex(writer, this.moduleRow.ModuleVersionId, metadataSizes.GuidIndexSize);
            SerializeIndex(writer, this.moduleRow.EncId, metadataSizes.GuidIndexSize);
            SerializeIndex(writer, this.moduleRow.EncBaseId, metadataSizes.GuidIndexSize);
        }

        private void SerializeEncLogTable(BinaryWriter writer)
        {
            foreach (EncLogRow encLog in this.encLogTable)
            {
                writer.WriteUint(encLog.Token);
                writer.WriteUint((uint)encLog.FuncCode);
            }
        }

        private void SerializeEncMapTable(BinaryWriter writer)
        {
            foreach (EncMapRow encMap in this.encMapTable)
            {
                writer.WriteUint(encMap.Token);
            }
        }

        private void SerializeTypeRefTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (TypeRefRow typeRef in this.typeRefTable)
            {
                SerializeIndex(writer, typeRef.ResolutionScope, metadataSizes.ResolutionScopeCodedIndexSize);
                this.SerializeIndex(writer, typeRef.Name, metadataSizes.StringIndexSize);
                this.SerializeIndex(writer, typeRef.Namespace, metadataSizes.StringIndexSize);
            }
        }

        private void SerializeTypeDefTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (TypeDefRow typeDef in this.typeDefTable)
            {
                writer.WriteUint(typeDef.Flags);
                this.SerializeIndex(writer, typeDef.Name, metadataSizes.StringIndexSize);
                this.SerializeIndex(writer, typeDef.Namespace, metadataSizes.StringIndexSize);
                SerializeIndex(writer, typeDef.Extends, metadataSizes.TypeDefOrRefCodedIndexSize);
                SerializeIndex(writer, typeDef.FieldList, metadataSizes.FieldDefIndexSize);
                SerializeIndex(writer, typeDef.MethodList, metadataSizes.MethodDefIndexSize);
            }
        }

        private void SerializeFieldTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (FieldDefRow fieldDef in this.fieldDefTable)
            {
                writer.WriteUshort(fieldDef.Flags);
                this.SerializeIndex(writer, fieldDef.Name, metadataSizes.StringIndexSize);
                SerializeIndex(writer, fieldDef.Signature, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeIndex(BinaryWriter writer, StringIdx index, byte indexSize)
        {
            SerializeIndex(writer, index.Resolve(this.stringIndexMap), indexSize);
        }

        private static void SerializeIndex(BinaryWriter writer, uint index, byte indexSize)
        {
            if (indexSize == 2)
            {
                Debug.Assert((ushort)index == index);
                writer.WriteUshort((ushort)index);
            }
            else
            {
                writer.WriteUint(index);
            }
        }

        private void SerializeMethodTable(BinaryWriter writer, MetadataSizes metadataSizes, int methodBodyStreamRva)
        {
            foreach (MethodRow method in this.methodTable)
            {
                if (method.Rva == uint.MaxValue)
                {
                    writer.WriteUint(0);
                }
                else 
                {
                    writer.WriteUint((uint)methodBodyStreamRva + method.Rva);
                }

                writer.WriteUshort(method.ImplFlags);
                writer.WriteUshort(method.Flags);
                this.SerializeIndex(writer, method.Name, metadataSizes.StringIndexSize);
                SerializeIndex(writer, method.Signature, metadataSizes.BlobIndexSize);
                SerializeIndex(writer, method.ParamList, metadataSizes.ParameterIndexSize);
            }
        }

        private void SerializeParamTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (ParamRow param in this.paramTable)
            {
                writer.WriteUshort(param.Flags);
                writer.WriteUshort(param.Sequence);
                this.SerializeIndex(writer, param.Name, metadataSizes.StringIndexSize);
            }
        }

        private void SerializeInterfaceImplTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (InterfaceImplRow interfaceImpl in this.interfaceImplTable)
            {
                SerializeIndex(writer, interfaceImpl.Class, metadataSizes.TypeDefIndexSize);
                SerializeIndex(writer, interfaceImpl.Interface, metadataSizes.TypeDefOrRefCodedIndexSize);
            }
        }

        private void SerializeMemberRefTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (MemberRefRow memberRef in this.memberRefTable)
            {
                SerializeIndex(writer, memberRef.Class, metadataSizes.MemberRefParentCodedIndexSize);
                SerializeIndex(writer, memberRef.Name, metadataSizes.StringIndexSize);
                SerializeIndex(writer, memberRef.Signature, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeConstantTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (ConstantRow constant in this.constantTable)
            {
                writer.WriteByte(constant.Type);
                writer.WriteByte(0);
                SerializeIndex(writer, constant.Parent, metadataSizes.HasConstantCodedIndexSize);
                SerializeIndex(writer, constant.Value, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeCustomAttributeTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (CustomAttributeRow customAttribute in this.customAttributeTable)
            {
                SerializeIndex(writer, customAttribute.Parent, metadataSizes.HasCustomAttributeCodedIndexSize);
                SerializeIndex(writer, customAttribute.Type, metadataSizes.CustomAttributeTypeCodedIndexSize);
                SerializeIndex(writer, customAttribute.Value, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeFieldMarshalTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (FieldMarshalRow fieldMarshal in this.fieldMarshalTable)
            {
                SerializeIndex(writer, fieldMarshal.Parent, metadataSizes.HasFieldMarshalCodedIndexSize);
                SerializeIndex(writer, fieldMarshal.NativeType, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeDeclSecurityTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (DeclSecurityRow declSecurity in this.declSecurityTable)
            {
                writer.WriteUshort(declSecurity.Action);
                SerializeIndex(writer, declSecurity.Parent, metadataSizes.DeclSecurityCodedIndexSize);
                SerializeIndex(writer, declSecurity.PermissionSet, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeClassLayoutTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (ClassLayoutRow classLayout in this.classLayoutTable)
            {
                writer.WriteUshort(classLayout.PackingSize);
                writer.WriteUint(classLayout.ClassSize);
                SerializeIndex(writer, classLayout.Parent, metadataSizes.TypeDefIndexSize);
            }
        }

        private void SerializeFieldLayoutTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (FieldLayoutRow fieldLayout in this.fieldLayoutTable)
            {
                writer.WriteUint(fieldLayout.Offset);
                SerializeIndex(writer, fieldLayout.Field, metadataSizes.FieldDefIndexSize);
            }
        }

        private void SerializeStandAloneSigTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (uint blobIndex in this.GetStandAloneSignatures())
            {
                SerializeIndex(writer, blobIndex, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeEventMapTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (EventMapRow eventMap in this.eventMapTable)
            {
                SerializeIndex(writer, eventMap.Parent, metadataSizes.TypeDefIndexSize);
                SerializeIndex(writer, eventMap.EventList, metadataSizes.EventDefIndexSize);
            }
        }

        private void SerializeEventTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (EventRow eventRow in this.eventTable)
            {
                writer.WriteUshort(eventRow.EventFlags);
                SerializeIndex(writer, eventRow.Name, metadataSizes.StringIndexSize);
                SerializeIndex(writer, eventRow.EventType, metadataSizes.TypeDefOrRefCodedIndexSize);
            }
        }

        private void SerializePropertyMapTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (PropertyMapRow propertyMap in this.propertyMapTable)
            {
                SerializeIndex(writer, propertyMap.Parent, metadataSizes.TypeDefIndexSize);
                SerializeIndex(writer, propertyMap.PropertyList, metadataSizes.PropertyDefIndexSize);
            }
        }

        private void SerializePropertyTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (PropertyRow property in this.propertyTable)
            {
                writer.WriteUshort(property.PropFlags);
                this.SerializeIndex(writer, property.Name, metadataSizes.StringIndexSize);
                SerializeIndex(writer, property.Type, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeMethodSemanticsTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (MethodSemanticsRow methodSemantic in this.methodSemanticsTable)
            {
                writer.WriteUshort(methodSemantic.Semantic);
                SerializeIndex(writer, methodSemantic.Method, metadataSizes.MethodDefIndexSize);
                SerializeIndex(writer, methodSemantic.Association, metadataSizes.HasSemanticsCodedIndexSize);
            }
        }

        private void SerializeMethodImplTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (MethodImplRow methodImpl in this.methodImplTable)
            {
                SerializeIndex(writer, methodImpl.Class, metadataSizes.TypeDefIndexSize);
                SerializeIndex(writer, methodImpl.MethodBody, metadataSizes.MethodDefOrRefCodedIndexSize);
                SerializeIndex(writer, methodImpl.MethodDecl, metadataSizes.MethodDefOrRefCodedIndexSize);
            }
        }

        private void SerializeModuleRefTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (ModuleRefRow moduleRef in this.moduleRefTable)
            {
                this.SerializeIndex(writer, moduleRef.Name, metadataSizes.StringIndexSize);
            }
        }

        private void SerializeTypeSpecTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (TypeSpecRow typeSpec in this.typeSpecTable)
            {
                SerializeIndex(writer, typeSpec.Signature, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeImplMapTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (ImplMapRow implMap in this.implMapTable)
            {
                writer.WriteUshort(implMap.MappingFlags);
                SerializeIndex(writer, implMap.MemberForwarded, metadataSizes.MemberForwardedCodedIndexSize);
                this.SerializeIndex(writer, implMap.ImportName, metadataSizes.StringIndexSize);
                SerializeIndex(writer, implMap.ImportScope, metadataSizes.ModuleRefIndexSize);
            }
        }

        private void SerializeFieldRvaTable(BinaryWriter writer, MetadataSizes metadataSizes, int mappedFieldDataStreamRva)
        {
            foreach (FieldRvaRow fieldRva in this.fieldRvaTable)
            {
                writer.WriteUint((uint)mappedFieldDataStreamRva + fieldRva.Offset);
                SerializeIndex(writer, fieldRva.Field, metadataSizes.FieldDefIndexSize);
            }
        }

        private void SerializeAssemblyTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return;
            }

            writer.WriteUint((uint)assembly.HashAlgorithm);
            writer.WriteUshort((ushort)assembly.Version.Major);
            writer.WriteUshort((ushort)assembly.Version.Minor);
            writer.WriteUshort((ushort)assembly.Version.Build);
            writer.WriteUshort((ushort)assembly.Version.Revision);
            writer.WriteUint(assembly.Flags);
            SerializeIndex(writer, this.assemblyKey, metadataSizes.BlobIndexSize);
            this.SerializeIndex(writer, this.assemblyName, metadataSizes.StringIndexSize);
            this.SerializeIndex(writer, this.assemblyCulture, metadataSizes.StringIndexSize);
        }

        private void SerializeAssemblyRefTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (AssemblyRefTableRow assemblyRef in this.assemblyRefTable)
            {
                writer.WriteUshort((ushort)assemblyRef.Version.Major);
                writer.WriteUshort((ushort)assemblyRef.Version.Minor);
                writer.WriteUshort((ushort)assemblyRef.Version.Build);
                writer.WriteUshort((ushort)assemblyRef.Version.Revision);

                // flags: reference has token, not full public key
                uint flags = 0;
                if (assemblyRef.IsRetargetable)
                {
                    flags |= (uint)AssemblyFlags.Retargetable;
                }

                flags |= (uint)assemblyRef.ContentType << 9;

                writer.WriteUint(flags);

                SerializeIndex(writer, assemblyRef.PublicKeyToken, metadataSizes.BlobIndexSize);
                this.SerializeIndex(writer, assemblyRef.Name, metadataSizes.StringIndexSize);
                this.SerializeIndex(writer, assemblyRef.Culture, metadataSizes.StringIndexSize);
                SerializeIndex(writer, 0, metadataSizes.BlobIndexSize); // hash of referenced assembly. Omitted.
            }
        }

        private void SerializeFileTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (FileTableRow fileReference in this.fileTable)
            {
                writer.WriteUint(fileReference.Flags);
                this.SerializeIndex(writer, fileReference.FileName, metadataSizes.StringIndexSize);
                SerializeIndex(writer, fileReference.HashValue, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeExportedTypeTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (ExportedTypeRow exportedType in this.exportedTypeTable)
            {
                writer.WriteUint((uint)exportedType.Flags);
                writer.WriteUint(exportedType.TypeDefId);
                this.SerializeIndex(writer, exportedType.TypeName, metadataSizes.StringIndexSize);
                this.SerializeIndex(writer, exportedType.TypeNamespace, metadataSizes.StringIndexSize);
                SerializeIndex(writer, exportedType.Implementation, metadataSizes.ImplementationCodedIndexSize);
            }
        }

        private void SerializeManifestResourceTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (ManifestResourceRow manifestResource in this.manifestResourceTable)
            {
                writer.WriteUint(manifestResource.Offset);
                writer.WriteUint(manifestResource.Flags);
                this.SerializeIndex(writer, manifestResource.Name, metadataSizes.StringIndexSize);
                SerializeIndex(writer, manifestResource.Implementation, metadataSizes.ImplementationCodedIndexSize);
            }
        }

        private void SerializeNestedClassTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (NestedClassRow nestedClass in this.nestedClassTable)
            {
                SerializeIndex(writer, nestedClass.NestedClass, metadataSizes.TypeDefIndexSize);
                SerializeIndex(writer, nestedClass.EnclosingClass, metadataSizes.TypeDefIndexSize);
            }
        }

        private void SerializeGenericParamTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (GenericParamRow genericParam in this.genericParamTable)
            {
                writer.WriteUshort(genericParam.Number);
                writer.WriteUshort(genericParam.Flags);
                SerializeIndex(writer, genericParam.Owner, metadataSizes.TypeOrMethodDefCodedIndexSize);
                this.SerializeIndex(writer, genericParam.Name, metadataSizes.StringIndexSize);
            }
        }

        private void SerializeMethodSpecTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (MethodSpecRow methodSpec in this.methodSpecTable)
            {
                SerializeIndex(writer, methodSpec.Method, metadataSizes.MethodDefOrRefCodedIndexSize);
                SerializeIndex(writer, methodSpec.Instantiation, metadataSizes.BlobIndexSize);
            }
        }

        private void SerializeGenericParamConstraintTable(BinaryWriter writer, MetadataSizes metadataSizes)
        {
            foreach (GenericParamConstraintRow genericParamConstraint in this.genericParamConstraintTable)
            {
                SerializeIndex(writer, genericParamConstraint.Owner, metadataSizes.GenericParamIndexSize);
                SerializeIndex(writer, genericParamConstraint.Constraint, metadataSizes.TypeDefOrRefCodedIndexSize);
            }
        }
        
        private uint[] SerializeMethodBodies(BinaryWriter writer, PdbWriter pdbWriterOpt)
        {
            var customDebugInfoWriter = new CustomDebugInfoWriter();

            var methods = this.GetMethodDefs();
            uint[] rvas = new uint[methods.Count];
            
            int i = 0;
            foreach (IMethodDefinition method in methods)
            {
                cancellationToken.ThrowIfCancellationRequested();
                uint rva;

                if (method.HasBody())
                {
                    IMethodBody body = method.GetBody(Context);
                    Debug.Assert(body != null || allowMissingMethodBodies);

                    if (body != null)
                    {
                        uint localSignatureToken = this.SerializeLocalVariablesSignature(body);

                        // TODO: consider parallelizing these (local signature tokens can be piped into IL serialization & debug info generation)
                        rva = this.SerializeMethodBody(body, writer, localSignatureToken);

                        if (pdbWriterOpt != null)
                        {
                            pdbWriterOpt.SerializeDebugInfo(body, localSignatureToken, customDebugInfoWriter);
                        }
                    }
                    else
                    {
                        rva = 0;
                    }
                }
                else
                {
                    // 0 is actually written to metadata when the row is serialized
                    rva = uint.MaxValue;
                }

                rvas[i++] = rva;
            }

            return rvas;
        }

        private uint SerializeMethodBody(IMethodBody methodBody, BinaryWriter writer, uint localSignatureToken)
        {
            int ilLength = methodBody.IL.Length;
            uint numberOfExceptionHandlers = (uint)methodBody.ExceptionRegions.Length;
            bool isSmallBody = ilLength < 64 && methodBody.MaxStack <= 8 && localSignatureToken == 0 && numberOfExceptionHandlers == 0;

            byte[] il = this.SerializeMethodBodyIL(methodBody);

            // serialization only replaces fake tokens with real tokens, it doesn't remove/insert bytecodes:
            Debug.Assert(il.Length == ilLength);

            uint bodyRva;
            if (isSmallBody)
            {
                // Check if an identical method body has already been serialized. 
                // If so, use the RVA of the already serialized one.
                if (!smallMethodBodies.TryGetValue(il, out bodyRva))
                {
                    bodyRva = writer.BaseStream.Position;
                    smallMethodBodies.Add(il, bodyRva);
                }

                writer.WriteByte((byte)((ilLength << 2) | 2));
            }
            else
            {
                writer.Align(4);
                bodyRva = writer.BaseStream.Position;
                ushort flags = (3 << 12) | 0x3;
                if (numberOfExceptionHandlers > 0)
                {
                    flags |= 0x08;
                }

                if (methodBody.LocalsAreZeroed)
                {
                    flags |= 0x10;
                }

                writer.WriteUshort(flags);
                writer.WriteUshort(methodBody.MaxStack);
                writer.WriteUint((uint)ilLength);
                writer.WriteUint(localSignatureToken);
            }

            writer.WriteBytes(il);
            if (numberOfExceptionHandlers > 0)
            {
                this.SerializeMethodBodyExceptionHandlerTable(methodBody, numberOfExceptionHandlers, writer);
            }

            return bodyRva;
        }

        /// <summary>
        /// Serialize the method local signature to the blob.
        /// </summary>
        /// <returns>Standalone signature token</returns>
        protected virtual uint SerializeLocalVariablesSignature(IMethodBody body)
        {
            Debug.Assert(!this.tableIndicesAreComplete);

            var localVariables = body.LocalVariables;
            if (localVariables.Length == 0)
            {
                return 0;
            }

            MemoryStream stream = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.WriteByte(0x07);
            writer.WriteCompressedUInt((uint)localVariables.Length);
            foreach (ILocalDefinition local in localVariables)
            {
                this.SerializeLocalVariableSignature(writer, local);
            }

            uint blobIndex = this.GetBlobIndex(writer.BaseStream);
            uint signatureIndex = this.GetOrAddStandAloneSignatureIndex(blobIndex);
            stream.Free();

            return 0x11000000 | signatureIndex;
        }

        protected void SerializeLocalVariableSignature(BinaryWriter writer, ILocalDefinition local)
        {
            if (module.IsPlatformType(local.Type, PlatformType.SystemTypedReference))
            {
                writer.WriteByte(0x16);
            }
            else
            {
                foreach (ICustomModifier customModifier in local.CustomModifiers)
                {
                    this.SerializeCustomModifier(customModifier, writer);
                }

                if (local.IsPinned)
                {
                    writer.WriteByte(0x45);
                }

                if (local.IsReference)
                {
                    writer.WriteByte(0x10);
                }

                this.SerializeTypeReference(local.Type, writer, false, true);
            }
        }

        internal uint SerializeLocalConstantSignature(ILocalDefinition localConstant)
        {
            MemoryStream sig = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(sig);
            writer.WriteByte(0x06);

            foreach (ICustomModifier modifier in localConstant.CustomModifiers)
            {
                this.SerializeCustomModifier(modifier, writer);
            }

            this.SerializeTypeReference(localConstant.Type, writer, false, true);
            uint blobIndex = this.GetBlobIndex(sig);
            uint signatureIndex = GetOrAddStandAloneSignatureIndex(blobIndex);
            sig.Free();

            return 0x11000000 | signatureIndex;
        }

        private static uint ReadUint(byte[] buffer, int pos)
        {
            uint result = buffer[pos];
            result |= (uint)buffer[pos + 1] << 8;
            result |= (uint)buffer[pos + 2] << 16;
            result |= (uint)buffer[pos + 3] << 24;
            return result;
        }

        private static void WriteUint(byte[] buffer, uint value, int pos)
        {
            unchecked
            {
                buffer[pos] = (byte)value;
                buffer[pos + 1] = (byte)(value >> 8);
                buffer[pos + 2] = (byte)(value >> 16);
                buffer[pos + 3] = (byte)(value >> 24);
            }
        }

        private uint ResolveTokenFromReference(IReference reference)
        {
            ITypeReference typeReference = reference as ITypeReference;

            if (typeReference != null)
            {
                return this.GetTypeToken(typeReference);
            }
            else
            {
                IFieldReference fieldReference = reference as IFieldReference;

                if (fieldReference != null)
                {
                    return this.GetFieldToken(fieldReference);
                }
                else
                {
                    IMethodReference methodReference = reference as IMethodReference;
                    if (methodReference != null)
                    {
                        return this.GetMethodToken(methodReference);
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(reference);
                    }
                }
            }
        }

        private uint ResolveSymbolTokenFromPseudoSymbolToken(uint pseudoSymbolToken)
        {
            var index = (int)pseudoSymbolToken;
            var reference = pseudoSymbolTokenToReferenceMap[index];
            if (reference != null)
            {
                // EDMAURER since method bodies are not visited as they are in CCI, the operations
                // that would have been done on them are done here.
                this.referenceVisitor.VisitMethodBodyReference(reference);

                var token = ResolveTokenFromReference(reference);
                pseudoSymbolTokenToTokenMap[index] = token;
                pseudoSymbolTokenToReferenceMap[index] = null; // Set to null to bypass next lookup
                return token;
            }

            return pseudoSymbolTokenToTokenMap[index];
        }

        private uint ResolveStringTokenFromPseudoStringToken(uint pseudoStringToken)
        {
            var index = (int)pseudoStringToken;
            var str = pseudoStringTokenToStringMap[index];
            if (str != null)
            {
                var token = GetUserStringToken(str);
                pseudoStringTokenToTokenMap[index] = token;
                pseudoStringTokenToStringMap[index] = null; // Set to null to bypass next lookup
                return token;
            }

            return pseudoStringTokenToTokenMap[index];
        }

        private byte[] SerializeMethodBodyIL(IMethodBody methodBody)
        {
            // TODO: instead of writing into the byte[] on MethodBody we should write directly into MemoryStream
            byte[] methodBodyIL = methodBody.IL;

            int curIndex = 0;
            while (curIndex < methodBodyIL.Length)
            {
                OperandType operandType = InstructionOperandTypes.ReadOperandType(methodBodyIL, ref curIndex);
                switch (operandType)
                {
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        {
                            uint currentToken = ReadUint(methodBodyIL, curIndex);
                            uint newToken = ResolveSymbolTokenFromPseudoSymbolToken(currentToken);
                            WriteUint(methodBodyIL, newToken, curIndex);
                            curIndex += 4;
                        }
                        break;

                    case OperandType.InlineString:
                        {
                            uint currentToken = ReadUint(methodBodyIL, curIndex);
                            uint newToken = ResolveStringTokenFromPseudoStringToken(currentToken);
                            WriteUint(methodBodyIL, newToken, curIndex);
                            curIndex += 4;
                        }
                        break;

                    case OperandType.InlineSig: // Calli
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineI:
                    case OperandType.ShortInlineR:
                        curIndex += 4;
                        break;

                    case OperandType.InlineSwitch:
                        int argCount = (int)ReadUint(methodBodyIL, curIndex);
                        // skip switch arguments count and arguments
                        curIndex += (argCount + 1) * 4;
                        break;

                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        curIndex += 8;
                        break;

                    case OperandType.InlineNone:
                        break;

                    case OperandType.InlineVar:
                        curIndex += 2;
                        break;

                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        curIndex += 1;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(operandType);
                }
            }

            return methodBodyIL;
        }

        private void SerializeMethodBodyExceptionHandlerTable(IMethodBody methodBody, uint numberOfExceptionHandlers, BinaryWriter writer)
        {
            var regions = methodBody.ExceptionRegions;
            bool useSmallExceptionHeaders = MayUseSmallExceptionHeaders(numberOfExceptionHandlers, regions);
            writer.Align(4);
            if (useSmallExceptionHeaders)
            {
                uint dataSize = numberOfExceptionHandlers * 12 + 4;
                writer.WriteByte(0x01);
                writer.WriteByte((byte)(dataSize & 0xff));
                writer.WriteUshort(0);
            }
            else
            {
                uint dataSize = numberOfExceptionHandlers * 24 + 4;
                writer.WriteByte(0x41);
                writer.WriteByte((byte)(dataSize & 0xff));
                writer.WriteUshort((ushort)((dataSize >> 8) & 0xffff));
            }

            foreach (var region in regions)
            {
                this.SerializeExceptionRegion(region, useSmallExceptionHeaders, writer);
            }
        }

        private void SerializeExceptionRegion(ExceptionHandlerRegion region, bool useSmallExceptionHeaders, BinaryWriter writer)
        {
            writer.WriteUshort((ushort)region.HandlerKind);

            if (useSmallExceptionHeaders)
            {
                writer.WriteUshort((ushort)region.TryStartOffset);
                writer.WriteByte((byte)(region.TryEndOffset - region.TryStartOffset));
                writer.WriteUshort((ushort)region.HandlerStartOffset);
                writer.WriteByte((byte)(region.HandlerEndOffset - region.HandlerStartOffset));
            }
            else
            {
                writer.WriteUshort(0);
                writer.WriteUint(region.TryStartOffset);
                writer.WriteUint(region.TryEndOffset - region.TryStartOffset);
                writer.WriteUint(region.HandlerStartOffset);
                writer.WriteUint(region.HandlerEndOffset - region.HandlerStartOffset);
            }

            if (region.HandlerKind == ExceptionRegionKind.Catch)
            {
                writer.WriteUint(this.GetTypeToken(region.ExceptionType));
            }
            else
            {
                writer.WriteUint(region.FilterDecisionStartOffset);
            }
        }

        private static bool MayUseSmallExceptionHeaders(uint numberOfExceptionHandlers, ImmutableArray<ExceptionHandlerRegion> exceptionRegions)
        {
            if (numberOfExceptionHandlers * 12 + 4 > 0xff)
            {
                return false;
            }

            foreach (var region in exceptionRegions)
            {
                if (region.TryStartOffset > 0xffff)
                {
                    return false;
                }

                if (region.TryEndOffset - region.TryStartOffset > 0xff)
                {
                    return false;
                }

                if (region.HandlerStartOffset > 0xffff)
                {
                    return false;
                }

                if (region.HandlerEndOffset - region.HandlerStartOffset > 0xff)
                {
                    return false;
                }
            }

            return true;
        }

        private void SerializeParameterInformation(IParameterTypeInformation parameterTypeInformation, BinaryWriter writer)
        {
            bool hasByRefBeforeCustomModifiers = parameterTypeInformation.HasByRefBeforeCustomModifiers;

            Debug.Assert(!hasByRefBeforeCustomModifiers || parameterTypeInformation.IsByReference);

            if (hasByRefBeforeCustomModifiers && parameterTypeInformation.IsByReference)
            {
                writer.WriteByte(0x10);
            }

            foreach (ICustomModifier customModifier in parameterTypeInformation.CustomModifiers)
            {
                this.SerializeCustomModifier(customModifier, writer);
            }

            if (!hasByRefBeforeCustomModifiers && parameterTypeInformation.IsByReference)
            {
                writer.WriteByte(0x10);
            }

            this.SerializeTypeReference(parameterTypeInformation.GetType(Context), writer, false, true);
        }

        private void SerializeFieldSignature(IFieldReference fieldReference, BinaryWriter writer)
        {
            writer.WriteByte(0x06);

            this.SerializeTypeReference(fieldReference.GetType(Context), writer, false, true);
        }

        private void SerializeGenericMethodInstanceSignature(BinaryWriter writer, IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            writer.WriteByte(0x0a);
            writer.WriteCompressedUInt(genericMethodInstanceReference.GetGenericMethod(Context).GenericParameterCount);
            foreach (ITypeReference genericArgument in genericMethodInstanceReference.GetGenericArguments(Context))
            {
                this.SerializeTypeReference(genericArgument, writer, false, true);
            }
        }

        private void SerializeCustomAttributeSignature(ICustomAttribute customAttribute, bool writeOnlyNamedArguments, BinaryWriter writer)
        {
            if (!writeOnlyNamedArguments)
            {
                writer.WriteUshort(0x0001);
                var parameters = customAttribute.Constructor(Context).GetParameters(Context).GetEnumerator();
                foreach (var argument in customAttribute.GetArguments(Context))
                {
                    var success = parameters.MoveNext();
                    Debug.Assert(success);
                    if (!success)
                    {
                        // TODO: md error    
                        break;
                    }

                    this.SerializeMetadataExpression(writer, argument, parameters.Current.GetType(Context));
                }

                Debug.Assert(!parameters.MoveNext());

                writer.WriteUshort(customAttribute.NamedArgumentCount);
            }
            else
            {
                writer.WriteCompressedUInt(customAttribute.NamedArgumentCount);
            }

            if (customAttribute.NamedArgumentCount > 0)
            {
                foreach (IMetadataNamedArgument namedArgument in customAttribute.GetNamedArguments(Context))
                {
                    writer.WriteByte(namedArgument.IsField ? (byte)0x53 : (byte)0x54);
                    if (this.module.IsPlatformType(namedArgument.Type, PlatformType.SystemObject))
                    {
                        writer.WriteByte(0x51);
                    }
                    else
                    {
                        this.SerializeTypeReference(namedArgument.Type, writer, true, true);
                    }

                    writer.WriteString(namedArgument.ArgumentName, false);

                    this.SerializeMetadataExpression(writer, namedArgument.ArgumentValue, namedArgument.Type);
                }
            }
        }

        private void SerializeMetadataExpression(BinaryWriter writer, IMetadataExpression expression, ITypeReference targetType)
        {
            IMetadataCreateArray a = expression as IMetadataCreateArray;
            if (a != null)
            {
                ITypeReference targetElementType;
                var targetArrayType = targetType as IArrayTypeReference;

                if (targetArrayType == null)
                {
                    // implicit conversion from array to object
                    Debug.Assert(this.module.IsPlatformType(targetType, PlatformType.SystemObject));

                    targetElementType = a.ElementType;

                    writer.WriteByte(0x1d);
                    this.SerializeTypeReference(targetElementType, writer, true, true);
                }
                else
                {
                    targetElementType = targetArrayType.GetElementType(this.Context);
                }

                writer.WriteUint(a.ElementCount);

                foreach (IMetadataExpression elemValue in a.Elements)
                {
                    this.SerializeMetadataExpression(writer, elemValue, targetElementType);
                }
            }
            else
            {
                IMetadataConstant c = expression as IMetadataConstant;

                if (this.module.IsPlatformType(targetType, PlatformType.SystemObject))
                {
                    if (c != null &&
                        c.Value == null &&
                        this.module.IsPlatformType(c.Type, PlatformType.SystemObject))
                    {
                        // handle null case
                        writer.WriteByte(0x0e); // serialize string type
                        writer.WriteByte(0xFF); // null string
                        return;
                    }
                    else
                    {
                        this.SerializeTypeReference(expression.Type, writer, true, true);
                    }
                }

                if (c != null)
                {
                    if (c.Type is IArrayTypeReference)
                    {
                        writer.WriteInt(-1); // null array
                    }
                    else if (c.Type.TypeCode(Context) == PrimitiveTypeCode.String)
                    {
                        writer.WriteString((string)c.Value);
                    }
                    else if (this.module.IsPlatformType(c.Type, PlatformType.SystemType))
                    {
                        Debug.Assert(c.Value == null);
                        writer.WriteByte(0xFF); // null string
                    }
                    else
                    {
                        SerializeMetadataConstantValue(c.Value, writer);
                    }
                }
                else
                {
                    IMetadataTypeOf t = expression as IMetadataTypeOf;
                    if (t != null)
                    {
                        this.SerializeTypeName(t.TypeToGet, writer);
                    }
                    else
                    {
                        // TODO: error
                    }
                }
            }
        }

        private void SerializeMarshallingDescriptor(IMarshallingInformation marshallingInformation, BinaryWriter writer)
        {
            writer.WriteCompressedUInt((uint)marshallingInformation.UnmanagedType);
            switch (marshallingInformation.UnmanagedType)
            {
                case UnmanagedType.ByValArray: // NATIVE_TYPE_FIXEDARRAY
                    Debug.Assert(marshallingInformation.NumberOfElements >= 0);
                    writer.WriteCompressedUInt((uint)marshallingInformation.NumberOfElements);
                    if (marshallingInformation.ElementType >= 0)
                    {
                        writer.WriteCompressedUInt((uint)marshallingInformation.ElementType);
                    }

                    break;

                case Constants.UnmanagedType_CustomMarshaler:
                    writer.WriteUshort(0); // padding

                    object marshaller = marshallingInformation.GetCustomMarshaller(Context);
                    ITypeReference marshallerTypeRef = marshaller as ITypeReference;
                    if (marshallerTypeRef != null)
                    {
                        this.SerializeTypeName(marshallerTypeRef, writer);
                    }
                    else if (marshaller != null)
                    {
                        writer.WriteString((string)marshaller, false);
                    }
                    else
                    {
                        writer.WriteByte(0);
                    }

                    var arg = marshallingInformation.CustomMarshallerRuntimeArgument;
                    if (arg != null)
                    {
                        writer.WriteString(arg, false);
                    }
                    else
                    {
                        writer.WriteByte(0);
                    }

                    break;

                case UnmanagedType.LPArray: // NATIVE_TYPE_ARRAY
                    Debug.Assert(marshallingInformation.ElementType >= 0);
                    writer.WriteCompressedUInt((uint)marshallingInformation.ElementType);
                    if (marshallingInformation.ParamIndex >= 0)
                    {
                        writer.WriteCompressedUInt((uint)marshallingInformation.ParamIndex);
                        if (marshallingInformation.NumberOfElements >= 0)
                        {
                            writer.WriteCompressedUInt((uint)marshallingInformation.NumberOfElements);
                            writer.WriteByte(1); // The parameter number is valid
                        }
                    }
                    else if (marshallingInformation.NumberOfElements >= 0)
                    {
                        writer.WriteByte(0); // Dummy parameter value emitted so that NumberOfElements can be in a known position
                        writer.WriteCompressedUInt((uint)marshallingInformation.NumberOfElements);
                        writer.WriteByte(0); // The parameter number is not valid
                    }

                    break;

                case UnmanagedType.SafeArray:
                    if (marshallingInformation.SafeArrayElementSubtype >= 0)
                    {
                        writer.WriteCompressedUInt((uint)marshallingInformation.SafeArrayElementSubtype);
                        var elementType = marshallingInformation.GetSafeArrayElementUserDefinedSubtype(Context);
                        if (elementType != null)
                        {
                            this.SerializeTypeName(elementType, writer);
                        }
                    }

                    break;

                case UnmanagedType.ByValTStr: // NATIVE_TYPE_FIXEDSYSSTRING
                    writer.WriteCompressedUInt((uint)marshallingInformation.NumberOfElements);
                    break;

                case UnmanagedType.Interface:
                case UnmanagedType.IDispatch:
                case UnmanagedType.IUnknown:
                    if (marshallingInformation.IidParameterIndex >= 0)
                    {
                        writer.WriteCompressedUInt((uint)marshallingInformation.IidParameterIndex);
                    }

                    break;

                default:
                    break;
            }
        }

        private void SerializeTypeName(ITypeReference typeReference, BinaryWriter writer)
        {
            bool isAssemblyQualified = true;
            writer.WriteString(this.GetSerializedTypeName(typeReference, ref isAssemblyQualified), false);
        }

        private string GetSerializedTypeName(ITypeReference typeReference)
        {
            bool isAssemblyQualified = false;
            return this.GetSerializedTypeName(typeReference, ref isAssemblyQualified);
        }

        private string GetSerializedTypeName(ITypeReference typeReference, ref bool isAssemblyQualified)
        {
            StringBuilder sb = new StringBuilder();
            IArrayTypeReference arrType = typeReference as IArrayTypeReference;
            if (arrType != null)
            {
                typeReference = arrType.GetElementType(Context);
                bool isAssemQual = false;
                this.AppendSerializedTypeName(sb, typeReference, ref isAssemQual);
                if (arrType.IsVector)
                {
                    sb.Append("[]");
                }
                else
                {
                    sb.Append('[');
                    if (arrType.Rank == 1)
                    {
                        sb.Append('*');
                    }

                    for (int i = 1; i < arrType.Rank; i++)
                    {
                        sb.Append(',');
                    }

                    sb.Append(']');
                }

                goto done;
            }

            IPointerTypeReference pointer = typeReference as IPointerTypeReference;
            if (pointer != null)
            {
                typeReference = pointer.GetTargetType(Context);
                bool isAssemQual = false;
                this.AppendSerializedTypeName(sb, typeReference, ref isAssemQual);
                sb.Append('*');
                goto done;
            }

            IManagedPointerTypeReference reference = typeReference as IManagedPointerTypeReference;
            if (reference != null)
            {
                typeReference = reference.GetTargetType(Context);
                bool isAssemQual = false;
                this.AppendSerializedTypeName(sb, typeReference, ref isAssemQual);
                sb.Append('&');
                goto done;
            }

            INamespaceTypeReference namespaceType = typeReference.AsNamespaceTypeReference;
            if (namespaceType != null)
            {
                if (!(namespaceType.NamespaceName.Length == 0))
                {
                    sb.Append(namespaceType.NamespaceName);
                    sb.Append('.');
                }

                sb.Append(GetMangledAndEscapedName(namespaceType));
                goto done;
            }

            if (IsTypeSpecification(typeReference))
            {
                ITypeReference uninstantiatedTypeReference = GetUninstantiatedGenericType(typeReference);

                ArrayBuilder<ITypeReference> consolidatedTypeArguments = ArrayBuilder<ITypeReference>.GetInstance();
                GetConsolidatedTypeArguments(consolidatedTypeArguments, typeReference);

                sb.Append(this.GetSerializedTypeName(uninstantiatedTypeReference));
                sb.Append('[');
                bool first = true;
                foreach (ITypeReference argument in consolidatedTypeArguments)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    bool isAssemQual = true;
                    this.AppendSerializedTypeName(sb, argument, ref isAssemQual);
                }
                consolidatedTypeArguments.Free();

                sb.Append(']');
                goto done;
            }

            INestedTypeReference nestedType = typeReference.AsNestedTypeReference;
            if (nestedType != null)
            {
                sb.Append(this.GetSerializedTypeName(nestedType.GetContainingType(Context)));
                sb.Append('+');
                sb.Append(GetMangledAndEscapedName(nestedType));
                goto done;
            }

        // TODO: error
        done:
            if (isAssemblyQualified)
            {
                this.AppendAssemblyQualifierIfNecessary(sb, UnwrapTypeReference(typeReference), out isAssemblyQualified);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Strip off *, &amp;, and [].
        /// </summary>
        private ITypeReference UnwrapTypeReference(ITypeReference typeReference)
        {
            while (true)
            {
                IArrayTypeReference  arrType = typeReference as IArrayTypeReference;
                if (arrType != null)
                {
                    typeReference = arrType.GetElementType(Context);
                    continue;
                }

                IPointerTypeReference  pointer = typeReference as IPointerTypeReference;
                if (pointer != null)
                {
                    typeReference = pointer.GetTargetType(Context);
                    continue;
                }

                IManagedPointerTypeReference  reference = typeReference as IManagedPointerTypeReference;
                if (reference != null)
                {
                    typeReference = reference.GetTargetType(Context);
                    continue;
                }

                return typeReference;
            }
        }

        private void AppendAssemblyQualifierIfNecessary(StringBuilder sb, ITypeReference typeReference, out bool isAssemQualified)
        {
            INestedTypeReference nestedType = typeReference.AsNestedTypeReference;
            if (nestedType != null)
            {
                this.AppendAssemblyQualifierIfNecessary(sb, nestedType.GetContainingType(Context), out isAssemQualified);
                return;
            }

            IGenericTypeInstanceReference genInst = typeReference.AsGenericTypeInstanceReference;
            if (genInst != null)
            {
                this.AppendAssemblyQualifierIfNecessary(sb, genInst.GenericType, out isAssemQualified);
                return;
            }

            IArrayTypeReference arrType = typeReference as IArrayTypeReference;
            if (arrType != null)
            {
                this.AppendAssemblyQualifierIfNecessary(sb, arrType.GetElementType(Context), out isAssemQualified);
            }

            IPointerTypeReference pointer = typeReference as IPointerTypeReference;
            if (pointer != null)
            {
                this.AppendAssemblyQualifierIfNecessary(sb, pointer.GetTargetType(Context), out isAssemQualified);
            }

            IManagedPointerTypeReference reference = typeReference as IManagedPointerTypeReference;
            if (reference != null)
            {
                this.AppendAssemblyQualifierIfNecessary(sb, pointer.GetTargetType(Context), out isAssemQualified);
            }

            isAssemQualified = false;
            IAssemblyReference referencedAssembly = null;
            INamespaceTypeReference namespaceType = typeReference.AsNamespaceTypeReference;
            if (namespaceType != null)
            {
                referencedAssembly = namespaceType.GetUnit(Context) as IAssemblyReference;
            }

            if (referencedAssembly != null)
            {
                var containingAssembly = this.module.GetContainingAssembly(Context);

                if (containingAssembly == null || !ReferenceEquals(referencedAssembly, containingAssembly))
                {
                    sb.Append(", ");
                    sb.Append(StrongName(referencedAssembly));
                    isAssemQualified = true;
                }
            }
        }

        /// <summary>
        /// Computes the string representing the strong name of the given assembly reference.
        /// </summary>
        private static string StrongName(IAssemblyReference assemblyReference)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(assemblyReference.Name);
            sb.AppendFormat(CultureInfo.InvariantCulture, ", Version={0}.{1}.{2}.{3}", assemblyReference.Version.Major, assemblyReference.Version.Minor, assemblyReference.Version.Build, assemblyReference.Version.Revision);
            if (assemblyReference.Culture != null && assemblyReference.Culture.Length > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", Culture={0}", assemblyReference.Culture);
            }
            else
            {
                sb.Append(", Culture=neutral");
            }

            sb.Append(", PublicKeyToken=");
            if (IteratorHelper.EnumerableIsNotEmpty(assemblyReference.PublicKeyToken))
            {
                foreach (byte b in assemblyReference.PublicKeyToken)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            else
            {
                sb.Append("null");
            }

            if (assemblyReference.IsRetargetable)
            {
                sb.Append(", Retargetable=Yes");
            }

            return sb.ToString();
        }

        private void AppendSerializedTypeName(StringBuilder sb, ITypeReference type, ref bool isAssemQualified)
        {
            string argTypeName = this.GetSerializedTypeName(type, ref isAssemQualified);
            if (isAssemQualified)
            {
                sb.Append('[');
            }

            sb.Append(argTypeName);
            if (isAssemQualified)
            {
                sb.Append(']');
            }
        }

        private void SerializePermissionSet(IEnumerable<ICustomAttribute> permissionSet, BinaryWriter writer)
        {
            foreach (ICustomAttribute customAttribute in permissionSet)
            {
                bool isAssemblyQualified = true;
                string typeName = this.GetSerializedTypeName(customAttribute.GetType(Context), ref isAssemblyQualified);
                if (!isAssemblyQualified)
                {
                    IAssemblyReference referencedAssembly = null;
                    INamespaceTypeReference namespaceType = customAttribute.GetType(Context).AsNamespaceTypeReference;
                    if (namespaceType != null)
                    {
                        referencedAssembly = namespaceType.GetUnit(Context) as IAssemblyReference;
                        if (referencedAssembly != null)
                        {
                            typeName = typeName + ", " + StrongName(referencedAssembly);
                        }
                    }
                }

                writer.WriteString(typeName, false);
                BinaryWriter customAttributeWriter = new BinaryWriter(new MemoryStream());
                this.SerializeCustomAttributeSignature(customAttribute, true, customAttributeWriter);
                writer.WriteCompressedUInt(customAttributeWriter.BaseStream.Length);
                customAttributeWriter.BaseStream.WriteTo(writer.BaseStream);
            }

            // TODO: xml for older platforms
        }

        private void SerializeSignature(ISignature signature, ushort genericParameterCount, ImmutableArray<IParameterTypeInformation> extraArgumentTypes, BinaryWriter writer)
        {
            byte header = (byte)signature.CallingConvention;
            if (signature is IPropertyDefinition)
            {
                header |= 0x08;
            }

            writer.WriteByte(header);
            if (genericParameterCount > 0)
            {
                writer.WriteCompressedUInt(genericParameterCount);
            }

            var @params = signature.GetParameters(Context);
            uint numberOfRequiredParameters = (uint)@params.Length;
            uint numberOfOptionalParameters = (uint)extraArgumentTypes.Length;
            writer.WriteCompressedUInt(numberOfRequiredParameters + numberOfOptionalParameters);
            
            foreach (ICustomModifier customModifier in signature.ReturnValueCustomModifiers)
            {
                this.SerializeCustomModifier(customModifier, writer);
            }
        
            if (signature.ReturnValueIsByRef)
            {
                writer.WriteByte(0x10);
            }

            this.SerializeTypeReference(signature.GetType(Context), writer, false, true);
            foreach (IParameterTypeInformation parameterTypeInformation in @params)
            {
                this.SerializeParameterInformation(parameterTypeInformation, writer);
            }

            if (numberOfOptionalParameters > 0)
            {
                writer.WriteByte(0x41);
                foreach (IParameterTypeInformation extraArgumentTypeInformation in extraArgumentTypes)
                {
                    this.SerializeParameterInformation(extraArgumentTypeInformation, writer);
                }
            }
        }

        private void SerializeTypeReference(ITypeReference typeReference, BinaryWriter writer, bool noTokens, bool treatRefAsPotentialTypeSpec)
        {
            while (true)
            {
                var modifiedTypeReference = typeReference as IModifiedTypeReference;
                if (modifiedTypeReference != null)
                {
                    foreach (ICustomModifier customModifier in modifiedTypeReference.CustomModifiers)
                    {
                        this.SerializeCustomModifier(customModifier, writer);
                    }

                    typeReference = modifiedTypeReference.UnmodifiedType;
                }

                switch (typeReference.TypeCode(Context))
                {
                    case PrimitiveTypeCode.Void:
                        writer.WriteByte(0x01);
                        return;
                    case PrimitiveTypeCode.Boolean:
                        writer.WriteByte(0x02);
                        return;
                    case PrimitiveTypeCode.Char:
                        writer.WriteByte(0x03);
                        return;
                    case PrimitiveTypeCode.Int8:
                        writer.WriteByte(0x04);
                        return;
                    case PrimitiveTypeCode.UInt8:
                        writer.WriteByte(0x05);
                        return;
                    case PrimitiveTypeCode.Int16:
                        writer.WriteByte(0x06);
                        return;
                    case PrimitiveTypeCode.UInt16:
                        writer.WriteByte(0x07);
                        return;
                    case PrimitiveTypeCode.Int32:
                        writer.WriteByte(0x08);
                        return;
                    case PrimitiveTypeCode.UInt32:
                        writer.WriteByte(0x09);
                        return;
                    case PrimitiveTypeCode.Int64:
                        writer.WriteByte(0x0a);
                        return;
                    case PrimitiveTypeCode.UInt64:
                        writer.WriteByte(0x0b);
                        return;
                    case PrimitiveTypeCode.Float32:
                        writer.WriteByte(0x0c);
                        return;
                    case PrimitiveTypeCode.Float64:
                        writer.WriteByte(0x0d);
                        return;
                    case PrimitiveTypeCode.String:
                        writer.WriteByte(0x0e);
                        return;
                    case PrimitiveTypeCode.Pointer:
                        var pointerTypeReference = typeReference as IPointerTypeReference;
                        if (pointerTypeReference != null)
                        {
                            if (noTokens)
                            {
                                this.SerializeTypeName(pointerTypeReference, writer);
                                return;
                            }
                            else
                            {
                                writer.WriteByte(0x0f);
                                typeReference = pointerTypeReference.GetTargetType(Context);
                                noTokens = false;
                                treatRefAsPotentialTypeSpec = true;
                                continue;
                            }
                        }

                        break;
                    case PrimitiveTypeCode.Reference:
                        var managedPointerTypeReference = typeReference as IManagedPointerTypeReference;
                        if (managedPointerTypeReference != null)
                        {
                            if (noTokens)
                            {
                                this.SerializeTypeName(managedPointerTypeReference, writer);
                                return;
                            }
                            else
                            {
                                writer.WriteByte(0x10);
                                typeReference = managedPointerTypeReference.GetTargetType(Context);
                                noTokens = false;
                                treatRefAsPotentialTypeSpec = true;
                                continue;
                            }
                        }

                        break;
                    case PrimitiveTypeCode.IntPtr:
                        writer.WriteByte(0x18);
                        return;
                    case PrimitiveTypeCode.UIntPtr:
                        writer.WriteByte(0x19);
                        return;
                }

                IArrayTypeReference arrayTypeReference;
                IGenericMethodParameterReference genericMethodParameterReference;
                IGenericTypeParameterReference genericTypeParameterReference;

                if ((genericTypeParameterReference = typeReference.AsGenericTypeParameterReference) != null)
                {
                    writer.WriteByte(0x13);
                    uint numberOfInheritedParameters = GetNumberOfInheritedTypeParameters(genericTypeParameterReference.DefiningType);
                    writer.WriteCompressedUInt(numberOfInheritedParameters + genericTypeParameterReference.Index);
                    return;
                }
                else if ((arrayTypeReference = typeReference as IArrayTypeReference) != null && !arrayTypeReference.IsVector)
                {
                    Debug.Assert(noTokens == false, "Custom attributes cannot have multi-dimensional arrays");

                    writer.WriteByte(0x14);
                    this.SerializeTypeReference(arrayTypeReference.GetElementType(Context), writer, false, true);
                    writer.WriteCompressedUInt(arrayTypeReference.Rank);
                    writer.WriteCompressedUInt(IteratorHelper.EnumerableCount(arrayTypeReference.Sizes));
                    foreach (ulong size in arrayTypeReference.Sizes)
                    {
                        writer.WriteCompressedUInt((uint)size);
                    }

                    writer.WriteCompressedUInt(IteratorHelper.EnumerableCount(arrayTypeReference.LowerBounds));
                    foreach (int lowerBound in arrayTypeReference.LowerBounds)
                    {
                        writer.WriteCompressedSignedInteger(lowerBound);
                    }

                    return;
                }
                else if (module.IsPlatformType(typeReference, PlatformType.SystemTypedReference))
                {
                    writer.WriteByte(0x16);
                    return;
                }
                else if (module.IsPlatformType(typeReference, PlatformType.SystemObject))
                {
                    if (noTokens)
                    {
                        writer.WriteByte(0x51);
                    }
                    else
                    {
                        writer.WriteByte(0x1c);
                    }

                    return;
                }
                else if (arrayTypeReference != null && arrayTypeReference.IsVector)
                {
                    writer.WriteByte(0x1d);
                    typeReference = arrayTypeReference.GetElementType(Context);
                    treatRefAsPotentialTypeSpec = true;
                    continue;
                }
                else if ((genericMethodParameterReference = typeReference.AsGenericMethodParameterReference) != null)
                {
                    writer.WriteByte(0x1e);
                    writer.WriteCompressedUInt(genericMethodParameterReference.Index);
                    return;
                }
                else if (!noTokens && IsTypeSpecification(typeReference) && treatRefAsPotentialTypeSpec)
                {
                    ITypeReference uninstantiatedTypeReference = GetUninstantiatedGenericType(typeReference);

                    // Roslyn's uninstantiated type is the same object as the instantiated type for
                    // types closed over their type parameters, so to speak.

                    writer.WriteByte(0x15);
                    this.SerializeTypeReference(uninstantiatedTypeReference, writer, false, false);
                    ArrayBuilder<ITypeReference> consolidatedTypeArguments = ArrayBuilder<ITypeReference>.GetInstance();
                    GetConsolidatedTypeArguments(consolidatedTypeArguments, typeReference);
                    writer.WriteCompressedUInt((uint)consolidatedTypeArguments.Count);
                    foreach (ITypeReference typeArgument in consolidatedTypeArguments)
                    {
                        this.SerializeTypeReference(typeArgument, writer, false, true);
                    }
                    consolidatedTypeArguments.Free();

                    return;
                }

                if (noTokens)
                {
                    if (this.module.IsPlatformType(typeReference, PlatformType.SystemType))
                    {
                        writer.WriteByte(0x50);
                    }
                    else if (!typeReference.IsEnum)
                    {
                        writer.WriteByte(0x51);
                    }
                    else
                    {
                        writer.WriteByte(0x55);
                        this.SerializeTypeName(typeReference, writer);
                    }
                }
                else
                {
                    if (typeReference.IsValueType)
                    {
                        writer.WriteByte(0x11);
                    }
                    else
                    {
                        writer.WriteByte(0x12);
                    }

                    writer.WriteCompressedUInt(this.GetTypeDefOrRefCodedIndex(typeReference, treatRefAsPotentialTypeSpec));
                }
                return;
            }
        }

        private uint GetNumberOfInheritedTypeParameters(ITypeReference type)
        {
            INestedTypeReference nestedType = type.AsNestedTypeReference;
            if (nestedType == null)
            {
                return 0;
            }

            ISpecializedNestedTypeReference specializedNestedType = nestedType.AsSpecializedNestedTypeReference;
            if (specializedNestedType != null)
            {
                nestedType = specializedNestedType.UnspecializedVersion;
            }

            uint result = 0;
            type = nestedType.GetContainingType(Context);
            nestedType = type.AsNestedTypeReference;
            while (nestedType != null)
            {
                result += nestedType.GenericParameterCount;
                type = nestedType.GetContainingType(Context);
                nestedType = type.AsNestedTypeReference;
            }

            result += type.AsNamespaceTypeReference.GenericParameterCount;
            return result;
        }

        private void GetConsolidatedTypeArguments(ArrayBuilder<ITypeReference> consolidatedTypeArguments, ITypeReference typeReference)
        {
            INestedTypeReference nestedTypeReference = typeReference.AsNestedTypeReference;
            if (nestedTypeReference != null)
            {
                GetConsolidatedTypeArguments(consolidatedTypeArguments, nestedTypeReference.GetContainingType(Context));
            }

            IGenericTypeInstanceReference genTypeInstance = typeReference.AsGenericTypeInstanceReference;
            if (genTypeInstance != null)
            {
                consolidatedTypeArguments.AddRange(genTypeInstance.GetGenericArguments(Context));
            }
        }

        private static ITypeReference GetUninstantiatedGenericType(ITypeReference typeReference)
        {
            IGenericTypeInstanceReference genericTypeInstanceReference = typeReference.AsGenericTypeInstanceReference;
            if (genericTypeInstanceReference != null)
            {
                return genericTypeInstanceReference.GenericType;
            }

            ISpecializedNestedTypeReference specializedNestedType = typeReference.AsSpecializedNestedTypeReference;
            if (specializedNestedType != null)
            {
                return specializedNestedType.UnspecializedVersion;
            }

            return typeReference;
        }

        protected static uint RowOnly(uint token)
        {
            return token & 0xFFFFFF;
        }

        protected static uint TypeOnly(uint token)
        {
            return token & 0xFF000000;
        }

        protected abstract class HeapOrReferenceIndexBase<T>
        {
            private readonly MetadataWriter writer;
            private readonly List<T> rows;
            private readonly uint firstRowId;

            public HeapOrReferenceIndexBase(MetadataWriter writer, uint lastRowId)
            {
                this.writer = writer;
                this.rows = new List<T>();
                this.firstRowId = lastRowId + 1;
            }

            public abstract bool TryGetValue(T item, out uint index);

            public uint GetOrAdd(T item)
            {
                uint index;
                if (!this.TryGetValue(item, out index))
                {
                    index = Add(item);
                }
                return index;
            }

            public IReadOnlyList<T> Rows
            {
                get { return this.rows; }
            }

            public uint Add(T item)
            {
                Debug.Assert(!this.writer.tableIndicesAreComplete);
#if DEBUG
                uint i;
                Debug.Assert(!this.TryGetValue(item, out i));
#endif
                uint index = this.firstRowId + (uint)this.rows.Count;
                this.AddItem(item, index);
                this.rows.Add(item);
                return index;
            }

            protected abstract void AddItem(T item, uint index);
        }

        protected sealed class HeapOrReferenceIndex<T> : HeapOrReferenceIndexBase<T>
        {
            private readonly Dictionary<T, uint> index;

            public HeapOrReferenceIndex(MetadataWriter writer, uint lastRowId = 0) 
                : this(writer, new Dictionary<T, uint>(), lastRowId)
            {
            }

            public HeapOrReferenceIndex(MetadataWriter writer, IEqualityComparer<T> comparer, uint lastRowId = 0) 
                : this(writer, new Dictionary<T, uint>(comparer), lastRowId)
            {
            }

            private HeapOrReferenceIndex(MetadataWriter writer, Dictionary<T, uint> index, uint lastRowId) 
                : base(writer, lastRowId)
            {
                Debug.Assert(index.Count == 0);
                this.index = index;
            }

            public override bool TryGetValue(T item, out uint index)
            {
                return this.index.TryGetValue(item, out index);
            }

            protected override void AddItem(T item, uint index)
            {
                this.index.Add(item, index);
            }
        }

        protected sealed class InstanceAndStructuralReferenceIndex<T> : HeapOrReferenceIndexBase<T> where T : IReference
        {
            private readonly Dictionary<T, uint> instanceIndex;
            private readonly Dictionary<T, uint> structuralIndex;

            public InstanceAndStructuralReferenceIndex(MetadataWriter writer, IEqualityComparer<T> structuralComparer, uint lastRowId = 0) 
                : base(writer, lastRowId)
            {
                this.instanceIndex = new Dictionary<T, uint>();
                this.structuralIndex = new Dictionary<T, uint>(structuralComparer);
            }

            public override bool TryGetValue(T item, out uint index)
            {
                if (this.instanceIndex.TryGetValue(item, out index))
                {
                    return true;
                }
                if (this.structuralIndex.TryGetValue(item, out index))
                {
                    this.instanceIndex.Add(item, index);
                    return true;
                }
                return false;
            }

            protected override void AddItem(T item, uint index)
            {
                this.instanceIndex.Add(item, index);
                this.structuralIndex.Add(item, index);
            }
        }
    }
}
