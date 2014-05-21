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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal abstract class PeWriter
    {
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

        private static readonly Encoding Utf8Encoding = Encoding.UTF8;

        /// <summary>
        /// True if we should attempt to generate a deterministic output (no timestamps or random data).
        /// </summary>
        private readonly bool deterministic;

        protected PeWriter(
            EmitContext context,
            CommonMessageProvider messageProvider,
            PdbWriter pdbWriter,
            bool allowMissingMethodBodies,
            bool foldIdenticalMethodBodies,
            bool deterministic,
            CancellationToken cancellationToken)
        {
            this.module = context.Module;

            // EDMAURER provide some reasonable size estimates for these that will avoid
            // much of the reallocation that would occur when growing these from empty.
            signatureIndex = new Dictionary<ISignature, uint>(module.HintNumberOfMethodDefinitions); //ignores field signatures

            numTypeDefsEstimate = module.HintNumberOfMethodDefinitions / 6;
            exportedTypeIndex = new Dictionary<ITypeReference, uint>(numTypeDefsEstimate);
            exportedTypeList = new List<ITypeReference>(numTypeDefsEstimate);

            this.Context = context;
            this.messageProvider = messageProvider;
            this.emitRuntimeStartupStub = module.RequiresStartupStub;
            this.pdbWriter = pdbWriter;
            this.allowMissingMethodBodies = allowMissingMethodBodies;
            this.deterministic = deterministic;
            this.cancellationToken = cancellationToken;
            this.sizeOfImportAddressTable = this.emitRuntimeStartupStub ? (!this.module.Requires64bits ? 8u : 16u) : 0;

            if (pdbWriter != null)
            {
                pdbWriter.SetMetadataEmitter(this);
            }

            if (foldIdenticalMethodBodies)
            {
                possiblyDuplicateMethodBodies = new Dictionary<byte[], uint>(ByteSequenceComparer.Instance);
            }

            // Add zero-th entry to heaps. 
            // Delta metadata requires these to avoid nil generation-relative handles, 
            // which are technically viable but confusing.
            this.blobWriter.WriteByte(0);
            this.stringWriter.WriteByte(0);
            this.userStringWriter.WriteByte(0);
        }

        private readonly int numTypeDefsEstimate;
        private int NumberOfTypeDefsEstimate { get { return numTypeDefsEstimate; } }

        /// <summary>
        /// Returns true if writing full metadata, false if writing delta.
        /// </summary>
        private bool IsFullMetadata
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

        protected Guid ModuleVersionId
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
        /// The 1-based index of the generic parameter definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract uint GetGenericParameterIndex(IGenericParameter def);

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
        /// Invoked after serializing the method body.
        /// </summary>
        protected virtual void OnSerializedMethodBody(IMethodBody body)
        {
        }

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
        protected abstract void PopulateEncLogTableRows(List<EncLogRow> table);

        /// <summary>
        /// Populate EncMap table.
        /// </summary>
        protected abstract void PopulateEncMapTableRows(List<EncMapRow> table);

        // If true, it is allowed to have methods not have bodies (for emitting metadata-only
        // assembly)
        internal readonly bool allowMissingMethodBodies;
        private readonly CancellationToken cancellationToken;
        protected readonly IModule module;
        public readonly EmitContext Context;
        private readonly CommonMessageProvider messageProvider;

        // progress:
        private bool streamsAreComplete;
        private bool tableIndicesAreComplete;

        private uint[] pseudoSymbolTokenToTokenMap;
        private List<uint> pseudoStringTokenToTokenMap;

        private readonly ClrHeader clrHeader = new ClrHeader();

        private readonly bool emitRuntimeStartupStub;
        private readonly BinaryWriter coverageDataWriter = new BinaryWriter(new MemoryStream());
        private readonly SectionHeader coverSection = new SectionHeader();
        private PeDebugDirectory debugDirectory;
        private MemoryStream headerStream = new MemoryStream(1024);

        // #String heap
        private Dictionary<string, uint> stringIndex = new Dictionary<string, uint>(128);
        private Dictionary<uint, uint> stringIndexMap;
        protected readonly BinaryWriter stringWriter = new BinaryWriter(new MemoryStream(1024));

        // #US heap
        private readonly Dictionary<string, uint> userStringIndex = new Dictionary<string, uint>();
        protected readonly BinaryWriter userStringWriter = new BinaryWriter(new MemoryStream(1024), true);

        // #Blob heap
        private readonly Dictionary<byte[], uint> blobIndex = new Dictionary<byte[], uint>(ByteSequenceComparer.Instance);
        protected readonly BinaryWriter blobWriter = new BinaryWriter(new MemoryStream(1024));

        // #GUID heap
        private readonly Dictionary<Guid, uint> guidIndex = new Dictionary<Guid, uint>();
        protected readonly BinaryWriter guidWriter = new BinaryWriter(new MemoryStream(16)); // full metadata has just a single guid

        private readonly Dictionary<ICustomAttribute, uint> customAtributeSignatureIndex = new Dictionary<ICustomAttribute, uint>();
        private readonly MemoryStream emptyStream = new MemoryStream(0);
        private readonly Dictionary<ITypeReference, uint> typeSpecSignatureIndex = new Dictionary<ITypeReference, uint>();
        private readonly Dictionary<ITypeReference, uint> exportedTypeIndex;
        private readonly List<ITypeReference> exportedTypeList;
        private readonly Dictionary<string, uint> fileRefIndex = new Dictionary<string, uint>(32);  //more than enough in most cases
        private readonly List<IFileReference> fileRefList = new List<IFileReference>(32);
        private readonly Dictionary<IFieldReference, uint> fieldSignatureIndex = new Dictionary<IFieldReference, uint>();
        private readonly Dictionary<ISignature, uint> signatureIndex;
        private readonly Dictionary<IMarshallingInformation, uint> marshallingDescriptorIndex = new Dictionary<IMarshallingInformation, uint>();
        protected readonly List<IMethodImplementation> methodImplList = new List<IMethodImplementation>();
        private readonly Dictionary<IGenericMethodInstanceReference, uint> methodInstanceSignatureIndex = new Dictionary<IGenericMethodInstanceReference, uint>();
        // A map of method body to RVA. 
        private readonly Dictionary<byte[], uint> possiblyDuplicateMethodBodies;

        private byte blobIndexSize;
        private byte stringIndexSize;
        private byte guidIndexSize;

        private byte customAttributeTypeCodedIndexSize;
        private byte declSecurityCodedIndexSize;
        private byte eventDefIndexSize;
        private byte fieldDefIndexSize;
        private byte genericParamIndexSize;
        private byte hasConstantCodedIndexSize;
        private byte hasCustomAttributeCodedIndexSize;
        private byte hasFieldMarshalCodedIndexSize;
        private byte hasSemanticsCodedIndexSize;
        private byte implementationCodedIndexSize;
        private byte memberForwardedCodedIndexSize;
        private byte memberRefParentCodedIndexSize;
        private byte methodDefIndexSize;
        private byte methodDefOrRefCodedIndexSize;
        private byte moduleRefIndexSize;
        private byte parameterIndexSize;
        private byte propertyDefIndexSize;
        private byte resolutionScopeCodedIndexSize;
        private byte typeDefIndexSize;
        private byte typeDefOrRefCodedIndexSize;
        private byte typeOrMethodDefCodedIndexSize;

        private readonly uint[] tableSizes = new uint[MetadataTokens.TableCount];

        private readonly NtHeader ntHeader = new NtHeader();
        private readonly PdbWriter pdbWriter;
        private readonly BinaryWriter rdataWriter = new BinaryWriter(new MemoryStream());
        private readonly SectionHeader relocSection = new SectionHeader();
        private readonly SectionHeader resourceSection = new SectionHeader();
        private readonly BinaryWriter resourceWriter = new BinaryWriter(new MemoryStream(1024));
        private readonly BinaryWriter sdataWriter = new BinaryWriter(new MemoryStream());
        private readonly SectionHeader rdataSection = new SectionHeader();
        private readonly SectionHeader sdataSection = new SectionHeader();
        private readonly uint sizeOfImportAddressTable;
        private readonly BinaryWriter textDataWriter = new BinaryWriter(new MemoryStream());
        private readonly SectionHeader textSection = new SectionHeader();
        private readonly SectionHeader textDataSection = new SectionHeader();
        private readonly SectionHeader textMethodBodySection = new SectionHeader();
        private readonly SectionHeader tlsSection = new SectionHeader();
        private readonly BinaryWriter tlsDataWriter = new BinaryWriter(new MemoryStream());

        private readonly BinaryWriter win32ResourceWriter = new BinaryWriter(new MemoryStream(1024));

        // Well known dummy cor library types whose refs are used for attaching assembly attributes off within net modules
        // There is no guarantee the types actually exist in a cor library
        internal static readonly string dummyAssemblyAttributeParentNamespace = "System.Runtime.CompilerServices";
        internal static readonly string dummyAssemblyAttributeParentName = "AssemblyAttributesGoHere";
        internal static readonly string[,] dummyAssemblyAttributeParentQualifier = new string[2, 2] { { "", "M" }, { "S", "SM" } };
        private readonly uint[,] dummyAssemblyAttributeParent = new uint[2, 2] { { 0, 0 }, { 0, 0 } };

        private static readonly byte[] dosHeader = new byte[]
        {
            0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
            0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
            0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd,
            0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
            0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72,
            0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
            0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e,
            0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
            0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,
            0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

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

        public static void WritePeToStream(
            EmitContext context,
            CommonMessageProvider messageProvider,
            Stream stream,
            PdbWriter pdbWriter,
            bool allowMissingMethodBodies,
            bool foldDuplicateMethodBodies,
            bool deterministic,
            CancellationToken cancellationToken)
        {
            var writer = new FullPeWriter(context, messageProvider, pdbWriter, allowMissingMethodBodies, foldDuplicateMethodBodies, deterministic, cancellationToken);
            writer.WritePeToStream(stream);

            if (pdbWriter != null)
            {
                var entryPoint = context.Module.EntryPoint;

                if (entryPoint != null && entryPoint.GetResolvedMethod(context) != null)
                {
                    pdbWriter.SetEntryPoint(writer.GetMethodToken(entryPoint));
                }

                var assembly = context.Module.AsAssembly;
                if (assembly != null && assembly.Kind == ModuleKind.WindowsRuntimeMetadata)
                {
                    // Dev12: If compiling to winmdobj, we need to add to PDB source spans of
                    //        all types and members for better error reporting by WinMDExp.
                    pdbWriter.WriteDefinitionLocations(context.Module.GetSymbolToLocationMap());
                }
            }
        }

        internal void WritePeToStream(Stream peStream)
        {
            // Extract information from object model into tables, indices and streams
            CreateIndices();

            // TODO: we can precalculate the exact size of IL stream
            var ilBuffer = new MemoryStream(32 * 1024);
            var ilWriter = new BinaryWriter(ilBuffer);
            var metadataBuffer = new MemoryStream(16 * 1024);
            var metadataWriter = new BinaryWriter(metadataBuffer);

            // Since we are producing a full assembly, we should not have a module version ID
            // imposed ahead-of time. Instead we will compute a deterministic module version ID
            // based on the contents of the generated stream.
            Debug.Assert(this.ModuleVersionId == default(Guid));

            uint moduleVersionIdOffsetInMetadataStream;
            SerializeMetadataAndIL(metadataWriter, ilWriter, separateMethodIL: false, moduleVersionIdOffsetInMetadataStream: out moduleVersionIdOffsetInMetadataStream);

            // fill in header fields.
            FillInNtHeader((int)ilBuffer.Length);
            FillInClrHeader((int)ilBuffer.Length);

            // write to pe stream.
            long positionOfHeaderTimestamp;
            WriteHeaders(peStream, out positionOfHeaderTimestamp);
            long startOfMetadataStream;
            long positionOfDebugTableTimestamp;
            WriteTextSection(peStream, metadataBuffer, ilBuffer, out startOfMetadataStream, out positionOfDebugTableTimestamp);
            WriteRdataSection(peStream);
            WriteSdataSection(peStream);
            WriteCoverSection(peStream);
            WriteTlsSection(peStream);
            WriteResourceSection(peStream);
            WriteRelocSection(peStream);

            if (this.deterministic)
            {
                var positionOfModuleVersionId = startOfMetadataStream + moduleVersionIdOffsetInMetadataStream;
                WriteDeterministicGuidAndTimestamps(peStream, positionOfModuleVersionId, positionOfHeaderTimestamp, positionOfDebugTableTimestamp);
            }
        }

        internal void WriteMetadataAndIL(Stream metadataStream, Stream ilStream)
        {
            // Extract information from object model into tables, indices and streams
            CreateIndices();

            // TODO: we can precalculate the exact size of IL stream
            var ilBuffer = new MemoryStream(1024);
            var ilWriter = new BinaryWriter(ilBuffer);
            var metadataBuffer = new MemoryStream(4 * 1024);
            var metadataWriter = new BinaryWriter(metadataBuffer);

            // Add 4B of padding to the start of the separated IL stream, 
            // so that method RVAs, which are offsets to this stream, are never 0.
            ilWriter.WriteUint(0);

            // this is used to handle edit-and-continue emit, so we should have a module
            // version ID that is imposed by the caller (the same as the previous module version ID).
            // Therefore we do not have to fill in a new module version ID in the generated metadata
            // stream.
            Debug.Assert(this.ModuleVersionId != default(Guid));

            uint moduleVersionIdOffsetInMetadataStream;
            SerializeMetadataAndIL(metadataWriter, ilWriter, separateMethodIL: true, moduleVersionIdOffsetInMetadataStream: out moduleVersionIdOffsetInMetadataStream);

            ilBuffer.WriteTo(ilStream);
            metadataBuffer.WriteTo(metadataStream);
        }

        /// <summary>
        /// Compute a deterministic Guid and timestamp based on the contents of the stream, and replace
        /// the 16 zero bytes at the given position and one or two 4-byte values with that computed Guid and timestamp.
        /// </summary>
        /// <param name="stream">Stream of data</param>
        /// <param name="positionOfModuleVersionId">Position in the stream of 16 zero bytes to be replaced by a Guid</param>
        /// <param name="positionOfHeaderTimestamp">Position in the stream of four zero bytes to be replaced by a timestamp</param>
        /// <param name="positionOfDebugTableTimestamp">Position in the stream of four zero bytes to be replaced by a timestamp, or 0 if there is no second timestamp to be replaced</param>
        private static void WriteDeterministicGuidAndTimestamps(Stream stream, long positionOfModuleVersionId, long positionOfHeaderTimestamp, long positionOfDebugTableTimestamp)
        {
            var previousPosition = stream.Position;

            // The existing Guid in the data should be empty, as we are about to compute it.
            // Check to be sure.
            CheckZeroDataInStream(stream, positionOfModuleVersionId, 16);

            // Compute and write deterministic guid data over the relevant portion of the stream
            byte[] timestamp;
            var guidData = ComputeSerializedGuidFromData(stream, out timestamp);
            stream.Position = positionOfModuleVersionId;
            stream.Write(guidData, 0, 16);

            // Write a deterministic timestamp over the relevant portion(s) of the stream
            Debug.Assert(positionOfHeaderTimestamp != 0);
            CheckZeroDataInStream(stream, positionOfHeaderTimestamp, 4);
            stream.Position = positionOfHeaderTimestamp;
            stream.Write(timestamp, 0, 4);
            if (positionOfDebugTableTimestamp != 0)
            {
                CheckZeroDataInStream(stream, positionOfDebugTableTimestamp, 4);
                stream.Position = positionOfDebugTableTimestamp;
                stream.Write(timestamp, 0, 4);
            }

            stream.Position = previousPosition;
        }

        [Conditional("DEBUG")]
        private static void CheckZeroDataInStream(Stream stream, long position, int bytes)
        {
            stream.Position = position;
            for (int i = 0; i < bytes; i++)
            {
                int value = stream.ReadByte();
                Debug.Assert(value == 0, "Value not zero", "Value at index '{0}' was not zero", i);
            }
        }


        /// <summary>
        /// Compute a random-looking but deterministic Guid from a hash of the stream's data, and produce a "timestamp" from the remaining bits.
        /// </summary>
        private static byte[] ComputeSerializedGuidFromData(Stream stream, out byte[] timestamp)
        {
            stream.Position = 0; // rewind the stream

            var hashData = Hash.ComputeSha1(stream);
            var guidData = new byte[16];
            for (var i = 0; i < guidData.Length; i++)
            {
                guidData[i] = hashData[i];
            }

            // modify the guid data so it decodes to the form of a "random" guid ala rfc4122
            var t = guidData[7];
            t = (byte)((t & 0xf) | (4 << 4));
            guidData[7] = t;
            t = guidData[8];
            t = (byte)((t & 0x3f) | (2 << 6));
            guidData[8] = t;

            // compute a random-looking timestamp from the remaining bits, but with the upper bit set
            timestamp = new byte[4];
            timestamp[0] = hashData[16];
            timestamp[1] = hashData[17];
            timestamp[2] = hashData[18];
            timestamp[3] = (byte)(hashData[19] | 0x80);

            return guidData;
        }

        internal static uint Aligned(uint position, uint alignment)
        {
            uint result = position & ~(alignment - 1);
            if (result == position)
            {
                return result;
            }

            return result + alignment;
        }

        private uint ComputeStrongNameSignatureSize()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return 0;
            }

            // EDMAURER the count of characters divided by two because the each pair of characters will turn in to one byte.
            uint keySize = assembly.SignatureKey == null ? 0u : (uint)assembly.SignatureKey.Length / 2;

            if (keySize == 0)
                keySize = IteratorHelper.EnumerableCount(assembly.PublicKey);

            if (keySize == 0)
            {
                return 0;
            }

            return keySize < 128 + 32 ? 128u : keySize - 32;
        }

        private uint ComputeOffsetToDebugTable(int ilStreamLength)
        {
            uint result = this.ComputeOffsetToMetadata(ilStreamLength);
            result += this.ComputeSizeOfMetadata();
            result += Aligned(this.resourceWriter.BaseStream.Length, 4);
            result += this.ComputeStrongNameSignatureSize(); // size of strong name hash
            return result;
        }

        private uint ComputeOffsetToImportTable(int ilStreamLength)
        {
            uint result = this.ComputeOffsetToDebugTable(ilStreamLength);
            result += this.ComputeSizeOfDebugTable(result);
            result += 0; // TODO: size of unmanaged export stubs (when and if these are ever supported).
            return result;
        }

        private uint ComputeOffsetToMetadata(int ilStreamLength)
        {
            uint result = 0;
            result += this.sizeOfImportAddressTable;
            result += 72; // size of CLR header
            result += Aligned((uint)ilStreamLength, 4);
            return result;
        }

        private uint ComputeSizeOfDebugTable(uint offsetToMetadata)
        {
            if (this.pdbWriter == null)
            {
                return 0;
            }

            this.debugDirectory = this.pdbWriter.GetDebugDirectory();
            this.debugDirectory.TimeDateStamp = this.ntHeader.TimeDateStamp;
            this.debugDirectory.PointerToRawData = offsetToMetadata + 0x1c;
            return 0x1c + (uint)this.debugDirectory.Data.Length;
        }

        private uint ComputeSizeOfMetadata()
        {
            uint result = this.MetadataHeaderSize;
            result += Aligned(this.ComputeSizeOfMetadataTablesStream(), 4);
            result += Aligned(this.stringWriter.BaseStream.Length, 4);
            result += Aligned(this.userStringWriter.BaseStream.Length, 4);
            result += Aligned(this.guidWriter.BaseStream.Length, 4);
            result += Aligned(this.blobWriter.BaseStream.Length, 4);
            return result;
        }

        private uint ComputeSizeOfMetadataTablesStream()
        {
            this.ComputeColumnSizes();
            uint result = this.ComputeSizeOfTablesHeader();
            result += this.tableSizes[(byte)TableIndex.Module] * (2u + 3u * this.guidIndexSize + this.stringIndexSize);
            result += this.tableSizes[(byte)TableIndex.TypeRef] * (0u + this.resolutionScopeCodedIndexSize + this.stringIndexSize + this.stringIndexSize);
            result += this.tableSizes[(byte)TableIndex.TypeDef] * (4u + this.stringIndexSize + this.stringIndexSize + this.typeDefOrRefCodedIndexSize + this.fieldDefIndexSize + this.methodDefIndexSize);
            Debug.Assert(this.tableSizes[(byte)TableIndex.FieldPtr] == 0);
            result += this.tableSizes[(byte)TableIndex.Field] * (2u + this.stringIndexSize + this.blobIndexSize);
            Debug.Assert(this.tableSizes[(byte)TableIndex.MethodPtr] == 0);
            result += this.tableSizes[(byte)TableIndex.MethodDef] * (8u + this.stringIndexSize + this.blobIndexSize + this.parameterIndexSize);
            Debug.Assert(this.tableSizes[(byte)TableIndex.ParamPtr] == 0);
            result += this.tableSizes[(byte)TableIndex.Param] * (4u + this.stringIndexSize);
            result += this.tableSizes[(byte)TableIndex.InterfaceImpl] * (0u + this.typeDefIndexSize + this.typeDefOrRefCodedIndexSize);
            result += this.tableSizes[(byte)TableIndex.MemberRef] * (0u + this.memberRefParentCodedIndexSize + this.stringIndexSize + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.Constant] * (2u + this.hasConstantCodedIndexSize + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.CustomAttribute] * (0u + this.hasCustomAttributeCodedIndexSize + this.customAttributeTypeCodedIndexSize + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.FieldMarshal] * (0u + this.hasFieldMarshalCodedIndexSize + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.DeclSecurity] * (2u + this.declSecurityCodedIndexSize + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.ClassLayout] * (6u + this.typeDefIndexSize);
            result += this.tableSizes[(byte)TableIndex.FieldLayout] * (4u + this.fieldDefIndexSize);
            result += this.tableSizes[(byte)TableIndex.StandAloneSig] * (0u + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.EventMap] * (0u + this.typeDefIndexSize + this.eventDefIndexSize);
            Debug.Assert(this.tableSizes[(byte)TableIndex.EventPtr] == 0);
            result += this.tableSizes[(byte)TableIndex.Event] * (2u + this.stringIndexSize + this.typeDefOrRefCodedIndexSize);
            result += this.tableSizes[(byte)TableIndex.PropertyMap] * (0u + this.typeDefIndexSize + this.propertyDefIndexSize);
            Debug.Assert(this.tableSizes[(byte)TableIndex.PropertyPtr] == 0);
            result += this.tableSizes[(byte)TableIndex.Property] * (2u + this.stringIndexSize + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.MethodSemantics] * (2u + this.methodDefIndexSize + this.hasSemanticsCodedIndexSize);
            result += this.tableSizes[(byte)TableIndex.MethodImpl] * (0u + this.typeDefIndexSize + this.methodDefOrRefCodedIndexSize + this.methodDefOrRefCodedIndexSize);
            result += this.tableSizes[(byte)TableIndex.ModuleRef] * (0u + this.stringIndexSize);
            result += this.tableSizes[(byte)TableIndex.TypeSpec] * (0u + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.ImplMap] * (2u + this.memberForwardedCodedIndexSize + this.stringIndexSize + this.moduleRefIndexSize);
            result += this.tableSizes[(byte)TableIndex.FieldRva] * (4u + this.fieldDefIndexSize);
            result += this.tableSizes[(byte)TableIndex.EncLog] * (8u);
            result += this.tableSizes[(byte)TableIndex.EncMap] * (4u);
            result += this.tableSizes[(byte)TableIndex.Assembly] * (16u + this.blobIndexSize + this.stringIndexSize + this.stringIndexSize);
            Debug.Assert(this.tableSizes[(byte)TableIndex.AssemblyProcessor] == 0);
            Debug.Assert(this.tableSizes[(byte)TableIndex.AssemblyOS] == 0);
            result += this.tableSizes[(byte)TableIndex.AssemblyRef] * (12u + this.blobIndexSize + this.stringIndexSize + this.stringIndexSize + this.blobIndexSize);
            Debug.Assert(this.tableSizes[(byte)TableIndex.AssemblyRefProcessor] == 0);
            Debug.Assert(this.tableSizes[(byte)TableIndex.AssemblyRefOS] == 0);
            result += this.tableSizes[(byte)TableIndex.File] * (4u + this.stringIndexSize + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.ExportedType] * (8u + this.stringIndexSize + this.stringIndexSize + this.implementationCodedIndexSize);
            result += this.tableSizes[(byte)TableIndex.ManifestResource] * (8u + this.stringIndexSize + this.implementationCodedIndexSize);
            result += this.tableSizes[(byte)TableIndex.NestedClass] * (0u + this.typeDefIndexSize + this.typeDefIndexSize);
            result += this.tableSizes[(byte)TableIndex.GenericParam] * (4u + this.typeOrMethodDefCodedIndexSize + this.stringIndexSize);
            result += this.tableSizes[(byte)TableIndex.MethodSpec] * (0u + this.methodDefOrRefCodedIndexSize + this.blobIndexSize);
            result += this.tableSizes[(byte)TableIndex.GenericParamConstraint] * (0u + this.genericParamIndexSize + this.typeDefOrRefCodedIndexSize);
            return result + 1;
        }

        private uint ComputeSizeOfPeHeaders()
        {
            ushort numberOfSections = 1; // .text 
            if (this.emitRuntimeStartupStub) numberOfSections++; //.reloc
            if (this.tlsDataWriter.BaseStream.Length > 0) numberOfSections++; //.tls
            if (this.rdataWriter.BaseStream.Length > 0) numberOfSections++; //.rdata
            if (this.sdataWriter.BaseStream.Length > 0) numberOfSections++; //.sdata
            if (this.coverageDataWriter.BaseStream.Length > 0) numberOfSections++; //.cover
            if (!IteratorHelper.EnumerableIsEmpty(this.module.Win32Resources) ||
                this.module.Win32ResourceSection != null)
                numberOfSections++; //.rsrc;

            this.ntHeader.NumberOfSections = numberOfSections;
            uint sizeOfPeHeaders = 128 + 4 + 20 + 224 + 40u * numberOfSections;
            if (this.module.Requires64bits)
            {
                sizeOfPeHeaders += 16;
            }

            return sizeOfPeHeaders;
        }

        private uint ComputeSizeOfTextSection(int ilStreamLength)
        {
            uint textSectionLength = this.ComputeOffsetToImportTable(ilStreamLength);

            if (this.emitRuntimeStartupStub)
            {
                textSectionLength += !this.module.Requires64bits ? 66u : 70u; //size of import table
                textSectionLength += 14; //size of name table
                textSectionLength = Aligned(textSectionLength, !this.module.Requires64bits ? 4u : 8u); //optional padding to make startup stub's target address align on word or double word boundary
                textSectionLength += !this.module.Requires64bits ? 8u : 16u; //fixed size of runtime startup stub
            }

            textSectionLength += Aligned(this.textDataWriter.BaseStream.Length, 4);
            this.streamsAreComplete = true;
            return textSectionLength;
        }

        private uint ComputeSizeOfWin32Resources()
        {
            this.SerializeWin32Resources();
            uint result = 0;
            if (this.win32ResourceWriter.BaseStream.Length > 0)
            {
                result += Aligned(this.win32ResourceWriter.BaseStream.Length, 4);
            }            // result += Aligned(this.win32ResourceWriter.BaseStream.Length+1, 8);

            return result;
        }

        private void CreateMethodBodyReferenceIndex()
        {
            int count;
            var referencesInIL = module.ReferencesInIL(out count);

            this.pseudoSymbolTokenToTokenMap = new uint[count];
            uint cur = 0;
            foreach (IReference o in referencesInIL)
            {
                ITypeReference typeReference = o as ITypeReference;

                if (typeReference != null)
                {
                    this.pseudoSymbolTokenToTokenMap[cur] = this.GetTypeToken(typeReference);
                }
                else
                {
                    IFieldReference fieldReference = o as IFieldReference;

                    if (fieldReference != null)
                    {
                        this.pseudoSymbolTokenToTokenMap[cur] = this.GetFieldToken(fieldReference);
                    }
                    else
                    {
                        IMethodReference methodReference = o as IMethodReference;
                        if (methodReference != null)
                        {
                            this.pseudoSymbolTokenToTokenMap[cur] = this.GetMethodToken(methodReference);
                        }
                        else
                        {
                            throw ExceptionUtilities.UnexpectedValue(o);
                        }
                    }
                }

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

            // Find out all references. CCI used to do two passes: first without visiting attributes and the second including attributes.
            // The first pass helped to make type reference tokens be more like C#. We just need a single pass.
            var visitor = this.CreateReferenceVisitor();
            this.module.Dispatch(visitor);

            // EDMAURER since method bodies are not visited as they are in CCI, the operations
            // that would have been done on them are done here.
            visitor.VisitMethodBodyTypes(this.module);

            this.CreateMethodBodyReferenceIndex();
        }

        private void CreateUserStringIndices()
        {
            this.pseudoStringTokenToTokenMap = new List<uint>();

            foreach (string str in this.module.GetStrings())
            {
                this.pseudoStringTokenToTokenMap.Add(this.GetUserStringToken(str));
            }
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

        private void FillInClrHeader(int ilStreamLength)
        {
            ClrHeader clrHeader = this.clrHeader;
            clrHeader.CodeManagerTable.RelativeVirtualAddress = 0;
            clrHeader.CodeManagerTable.Size = 0;

            IMethodReference entryPoint = this.module.EntryPoint;
            if (entryPoint == null || entryPoint.GetResolvedMethod(Context) == null)
            {
                clrHeader.EntryPointToken = 0;
            }
            else
            {
                clrHeader.EntryPointToken = this.GetMethodToken(entryPoint);
            }

            clrHeader.ExportAddressTableJumps.RelativeVirtualAddress = 0;
            clrHeader.ExportAddressTableJumps.Size = 0;
            clrHeader.Flags = this.GetClrHeaderFlags();
            clrHeader.MajorRuntimeVersion = 2;
            clrHeader.MetaData.RelativeVirtualAddress = this.textSection.RelativeVirtualAddress + this.ComputeOffsetToMetadata(ilStreamLength);
            clrHeader.MetaData.Size = this.ComputeSizeOfMetadata();
            clrHeader.MinorRuntimeVersion = 5;
            clrHeader.Resources.RelativeVirtualAddress = clrHeader.MetaData.RelativeVirtualAddress + clrHeader.MetaData.Size;
            clrHeader.Resources.Size = Aligned(this.resourceWriter.BaseStream.Length, 4);
            clrHeader.StrongNameSignature.RelativeVirtualAddress = clrHeader.Resources.RelativeVirtualAddress + clrHeader.Resources.Size;
            clrHeader.StrongNameSignature.Size = this.ComputeStrongNameSignatureSize();
            clrHeader.VTableFixups.RelativeVirtualAddress = 0;
            clrHeader.VTableFixups.Size = 0;
        }

        private void FillInNtHeader(int ilStreamLength)
        {
            bool use32bitAddresses = !this.module.Requires64bits;
            NtHeader ntHeader = this.ntHeader;
            ntHeader.AddressOfEntryPoint = this.emitRuntimeStartupStub ? this.textDataSection.RelativeVirtualAddress - (use32bitAddresses ? 6u : 10u) : 0;
            ntHeader.BaseOfCode = this.textSection.RelativeVirtualAddress;
            ntHeader.BaseOfData = this.rdataSection.RelativeVirtualAddress;
            ntHeader.PointerToSymbolTable = 0;
            ntHeader.SizeOfCode = this.textSection.SizeOfRawData;
            ntHeader.SizeOfInitializedData = this.rdataSection.SizeOfRawData + this.coverSection.SizeOfRawData + this.sdataSection.SizeOfRawData + this.tlsSection.SizeOfRawData + this.resourceSection.SizeOfRawData + this.relocSection.SizeOfRawData;
            ntHeader.SizeOfHeaders = Aligned(this.ComputeSizeOfPeHeaders(), this.module.FileAlignment);
            ntHeader.SizeOfImage = Aligned(this.relocSection.RelativeVirtualAddress + this.relocSection.VirtualSize, 0x2000);
            ntHeader.SizeOfUninitializedData = 0;

            // In the PE File Header this is a "Time/Date Stamp" whose description is "Time and date
            // the file was created in seconds since January 1st 1970 00:00:00 or 0"
            // However, when we want to make it deterministic we fill it in (later) with bits from the hash of the full PE file.
            ntHeader.TimeDateStamp = deterministic ? 0 : (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            ntHeader.ImportAddressTable.RelativeVirtualAddress = (this.emitRuntimeStartupStub) ? this.textSection.RelativeVirtualAddress : 0;
            ntHeader.ImportAddressTable.Size = this.sizeOfImportAddressTable;
            ntHeader.CliHeaderTable.RelativeVirtualAddress = this.textSection.RelativeVirtualAddress + ntHeader.ImportAddressTable.Size;
            ntHeader.CliHeaderTable.Size = 72;
            ntHeader.ImportTable.RelativeVirtualAddress = this.textSection.RelativeVirtualAddress + this.ComputeOffsetToImportTable(ilStreamLength);

            if (!this.emitRuntimeStartupStub)
            {
                ntHeader.ImportTable.Size = 0;
                ntHeader.ImportTable.RelativeVirtualAddress = 0;
            }
            else
            {
                ntHeader.ImportTable.Size = use32bitAddresses ? 66u : 70u;
                ntHeader.ImportTable.Size += 13;  //size of nametable
            }

            ntHeader.BaseRelocationTable.RelativeVirtualAddress = (this.emitRuntimeStartupStub) ? this.relocSection.RelativeVirtualAddress : 0;
            ntHeader.BaseRelocationTable.Size = this.relocSection.VirtualSize;
            ntHeader.BoundImportTable.RelativeVirtualAddress = 0;
            ntHeader.BoundImportTable.Size = 0;
            ntHeader.CertificateTable.RelativeVirtualAddress = 0;
            ntHeader.CertificateTable.Size = 0;
            ntHeader.CopyrightTable.RelativeVirtualAddress = 0;
            ntHeader.CopyrightTable.Size = 0;
            ntHeader.DebugTable.RelativeVirtualAddress = this.pdbWriter == null ? 0u : this.textSection.RelativeVirtualAddress + this.ComputeOffsetToDebugTable(ilStreamLength);
            ntHeader.DebugTable.Size = this.pdbWriter == null ? 0u : 0x1c; // Only the size of the fixed part of the debug table goes here.
            ntHeader.DelayImportTable.RelativeVirtualAddress = 0;
            ntHeader.DelayImportTable.Size = 0;
            ntHeader.ExceptionTable.RelativeVirtualAddress = 0;
            ntHeader.ExceptionTable.Size = 0;
            ntHeader.ExportTable.RelativeVirtualAddress = 0;
            ntHeader.ExportTable.Size = 0;
            ntHeader.GlobalPointerTable.RelativeVirtualAddress = 0;
            ntHeader.GlobalPointerTable.Size = 0;
            ntHeader.LoadConfigTable.RelativeVirtualAddress = 0;
            ntHeader.LoadConfigTable.Size = 0;
            ntHeader.Reserved.RelativeVirtualAddress = 0;
            ntHeader.Reserved.Size = 0;
            ntHeader.ResourceTable.RelativeVirtualAddress = this.resourceSection.SizeOfRawData == 0 ? 0u : this.resourceSection.RelativeVirtualAddress;
            ntHeader.ResourceTable.Size = this.resourceSection.VirtualSize;
            ntHeader.ThreadLocalStorageTable.RelativeVirtualAddress = this.tlsSection.SizeOfRawData == 0 ? 0u : this.tlsSection.RelativeVirtualAddress;
            ntHeader.ThreadLocalStorageTable.Size = this.tlsSection.SizeOfRawData;
        }

        private void FillInSectionHeaders(int ilStreamLength)
        {
            uint sizeOfPeHeaders = this.ComputeSizeOfPeHeaders();
            uint sizeOfTextSection = this.ComputeSizeOfTextSection(ilStreamLength);

            this.textSection.Characteristics = 0x60000020; // section is read + execute + code 
            this.textSection.Name = ".text";
            this.textSection.NumberOfLinenumbers = 0;
            this.textSection.NumberOfRelocations = 0;
            this.textSection.PointerToLinenumbers = 0;
            this.textSection.PointerToRawData = Aligned(sizeOfPeHeaders, this.module.FileAlignment);
            this.textSection.PointerToRelocations = 0;
            this.textSection.RelativeVirtualAddress = Aligned(sizeOfPeHeaders, 0x2000);
            this.textSection.SizeOfRawData = Aligned(sizeOfTextSection, this.module.FileAlignment);
            this.textSection.VirtualSize = sizeOfTextSection;

            // Note: the textDataSection is not actually written out. Its data is appended to the text section.
            // The section exists to make it easier to use a single method to compute all field RVAs.
            this.textDataSection.RelativeVirtualAddress = this.textSection.RelativeVirtualAddress + this.textSection.VirtualSize - Aligned(this.textDataWriter.BaseStream.Length, 4);

            // likewise for the textMethodBodySection
            this.textMethodBodySection.RelativeVirtualAddress = this.textSection.RelativeVirtualAddress + this.sizeOfImportAddressTable + 72;

            this.rdataSection.Characteristics = 0x40000040; // section is read + initialized
            this.rdataSection.Name = ".rdata";
            this.rdataSection.NumberOfLinenumbers = 0;
            this.rdataSection.NumberOfRelocations = 0;
            this.rdataSection.PointerToLinenumbers = 0;
            this.rdataSection.PointerToRawData = this.textSection.PointerToRawData + this.textSection.SizeOfRawData;
            this.rdataSection.PointerToRelocations = 0;
            this.rdataSection.RelativeVirtualAddress = Aligned(this.textSection.RelativeVirtualAddress + this.textSection.VirtualSize, 0x2000);
            this.rdataSection.SizeOfRawData = Aligned(this.rdataWriter.BaseStream.Length, this.module.FileAlignment);
            this.rdataSection.VirtualSize = this.rdataWriter.BaseStream.Length;

            this.sdataSection.Characteristics = 0xC0000040; // section is write + read + initialized 
            this.sdataSection.Name = ".sdata";
            this.sdataSection.NumberOfLinenumbers = 0;
            this.sdataSection.NumberOfRelocations = 0;
            this.sdataSection.PointerToLinenumbers = 0;
            this.sdataSection.PointerToRawData = this.rdataSection.PointerToRawData + this.rdataSection.SizeOfRawData;
            this.sdataSection.PointerToRelocations = 0;
            this.sdataSection.RelativeVirtualAddress = Aligned(this.rdataSection.RelativeVirtualAddress + this.rdataSection.VirtualSize, 0x2000);
            this.sdataSection.SizeOfRawData = Aligned(this.sdataWriter.BaseStream.Length, this.module.FileAlignment);
            this.sdataSection.VirtualSize = this.sdataWriter.BaseStream.Length;

            this.coverSection.Characteristics = 0xC8000040; // section is not paged + write + read + initialized 
            this.coverSection.Name = ".cover";
            this.coverSection.NumberOfLinenumbers = 0;
            this.coverSection.NumberOfRelocations = 0;
            this.coverSection.PointerToLinenumbers = 0;
            this.coverSection.PointerToRawData = this.sdataSection.PointerToRawData + this.sdataSection.SizeOfRawData;
            this.coverSection.PointerToRelocations = 0;
            this.coverSection.RelativeVirtualAddress = Aligned(this.sdataSection.RelativeVirtualAddress + this.sdataSection.VirtualSize, 0x2000);
            this.coverSection.SizeOfRawData = Aligned(this.coverageDataWriter.BaseStream.Length, this.module.FileAlignment);
            this.coverSection.VirtualSize = this.coverageDataWriter.BaseStream.Length;

            this.tlsSection.Characteristics = 0xC0000040; // section is write + read + initialized 
            this.tlsSection.Name = ".tls";
            this.tlsSection.NumberOfLinenumbers = 0;
            this.tlsSection.NumberOfRelocations = 0;
            this.tlsSection.PointerToLinenumbers = 0;
            this.tlsSection.PointerToRawData = this.coverSection.PointerToRawData + this.coverSection.SizeOfRawData;
            this.tlsSection.PointerToRelocations = 0;
            this.tlsSection.RelativeVirtualAddress = Aligned(this.coverSection.RelativeVirtualAddress + this.coverSection.VirtualSize, 0x2000);
            this.tlsSection.SizeOfRawData = Aligned(this.tlsDataWriter.BaseStream.Length, this.module.FileAlignment);
            this.tlsSection.VirtualSize = this.tlsDataWriter.BaseStream.Length;

            this.resourceSection.Characteristics = 0x40000040; // section is read + initialized  
            this.resourceSection.Name = ".rsrc";
            this.resourceSection.NumberOfLinenumbers = 0;
            this.resourceSection.NumberOfRelocations = 0;
            this.resourceSection.PointerToLinenumbers = 0;
            this.resourceSection.PointerToRawData = this.tlsSection.PointerToRawData + this.tlsSection.SizeOfRawData;
            this.resourceSection.PointerToRelocations = 0;
            this.resourceSection.RelativeVirtualAddress = Aligned(this.tlsSection.RelativeVirtualAddress + this.tlsSection.VirtualSize, 0x2000);
            uint sizeOfWin32Resources = this.ComputeSizeOfWin32Resources();
            this.resourceSection.SizeOfRawData = Aligned(sizeOfWin32Resources, this.module.FileAlignment);
            this.resourceSection.VirtualSize = sizeOfWin32Resources;

            this.relocSection.Characteristics = 0x42000040; // section is read + discardable + initialized  
            this.relocSection.Name = ".reloc";
            this.relocSection.NumberOfLinenumbers = 0;
            this.relocSection.NumberOfRelocations = 0;
            this.relocSection.PointerToLinenumbers = 0;
            this.relocSection.PointerToRawData = this.resourceSection.PointerToRawData + this.resourceSection.SizeOfRawData;
            this.relocSection.PointerToRelocations = 0;
            this.relocSection.RelativeVirtualAddress = Aligned(this.resourceSection.RelativeVirtualAddress + this.resourceSection.VirtualSize, 0x2000);

            if (!this.emitRuntimeStartupStub)
            {
                this.relocSection.SizeOfRawData = 0;
                this.relocSection.VirtualSize = 0;
            }
            else
            {
                this.relocSection.SizeOfRawData = this.module.FileAlignment;
                this.relocSection.VirtualSize = this.module.Requires64bits && !this.module.RequiresAmdInstructionSet ? 14u : 12u;
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

        private uint GetBlobIndex(byte[] blob)
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

        private uint GetBlobIndex(object value)
        {
            string str = value as string;
            if (str != null)
            {
                return this.GetBlobIndex(str);
            }

            MemoryStream sig = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(sig, true);
            SerializeMetadataConstantValue(value, writer);
            return this.GetBlobIndex(sig.ToArray());
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

            return this.GetBlobIndex(byteArray);
        }

        private uint GetClrHeaderFlags()
        {
            uint result = 0;
            if (this.module.ILOnly)
                result |= 1;

            if (this.module.Requires32bits)
                result |= 2;

            if (this.module.StrongNameSigned)
                result |= 8;

            if (this.module.TrackDebugData)
                result |= 0x10000;

            if (this.module.Prefers32bits)
            {
                result |= 0x20000;
                result |= 2;
            }

            return result;
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
            result = this.GetBlobIndex(sig.ToArray());
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

        private uint GetDataOffset(ISectionBlock sectionBlock)
        {
            BinaryWriter sectionWriter;
            switch (sectionBlock.PESectionKind)
            {
                case PESectionKind.ConstantData:
                    sectionWriter = this.rdataWriter;
                    break;
                case PESectionKind.CoverageData:
                    sectionWriter = this.coverageDataWriter;
                    break;
                case PESectionKind.StaticData:
                    sectionWriter = this.sdataWriter;
                    break;
                case PESectionKind.Text:
                    sectionWriter = this.textDataWriter;
                    break;
                case PESectionKind.ThreadLocalStorage:
                    sectionWriter = this.tlsDataWriter;
                    break;
                default:
                    // TODO: error
                    goto case PESectionKind.Text;
            }

            if (sectionBlock.PESectionKind != PESectionKind.Text)
            {
                sectionWriter.BaseStream.Position = sectionBlock.Offset;
            }

            uint result = sectionWriter.BaseStream.Position;
            sectionWriter.WriteBytes(new List<byte>(sectionBlock.Data).ToArray());
            if (sectionWriter.BaseStream.Position == sectionWriter.BaseStream.Length)
            {
                sectionWriter.Align(8);
            }

            return result;
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

            if (fieldDef.IsMapped)
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
            result = this.GetBlobIndex(sig.ToArray());
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

        private uint GetManagedResourceOffset(ManagedResource resource)
        {
            Debug.Assert(!this.streamsAreComplete);
            if (resource.ExternalFile != null)
            {
                return resource.Offset;
            }

            uint result = this.resourceWriter.BaseStream.Position;
            resource.WriteData(this.resourceWriter);
            return result;
        }

        public static string GetMangledName(INamedTypeReference namedType)
        {
            string unmangledName = namedType.Name;
            if (!namedType.MangleName)
            {
                return unmangledName;
            }

            if (namedType.GenericParameterCount == 0)
            {
                return unmangledName;
            }

            return MetadataHelpers.ComposeAritySuffixedMetadataName(unmangledName, namedType.GenericParameterCount);
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

            result = this.GetBlobIndex(sig.ToArray());
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
            result = this.GetBlobIndex(sig.ToArray());
            this.marshallingDescriptorIndex.Add(marshallingInformation, result);
            sig.Free();
            return result;
        }

        private uint GetMarshallingDescriptorIndex(ImmutableArray<byte> descriptor)
        {
            return this.GetBlobIndex(descriptor.ToArray());
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
            result = this.GetBlobIndex(sig.ToArray());
            this.signatureIndex.Add(methodReference, result);
            sig.Free();
            return result;
        }

        unsafe internal byte[] GetMethodSignature(IMethodReference methodReference)
        {
            int signatureOffset = (int)GetMethodSignatureIndex(methodReference);

            fixed (byte* ptr = this.blobWriter.BaseStream.Buffer)
            {
                var reader = new BlobReader((IntPtr)ptr + signatureOffset, (int)this.blobWriter.BaseStream.Length + (int)this.GetBlobStreamOffset() - signatureOffset);
                uint size;
                bool isValid = reader.TryReadCompressedUInt32(out size);
                Debug.Assert(isValid);
                return reader.ReadBytes((int)size);
            }
        }

        private uint GetGenericMethodInstanceIndex(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            MemoryStream sig = MemoryStream.GetInstance();
            BinaryWriter writer = new BinaryWriter(sig);
            this.SerializeGenericMethodInstanceSignature(writer, genericMethodInstanceReference);
            uint result = this.GetBlobIndex(sig.ToArray());
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
                result = this.GetBlobIndex(sig.ToArray());
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
            result = this.GetBlobIndex(sig.ToArray());
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

        private static uint GetRva(SectionHeader sectionHeader, uint offset)
        {
            return sectionHeader.RelativeVirtualAddress + offset;
        }

        private SectionHeader GetSection(PESectionKind section)
        {
            switch (section)
            {
                case PESectionKind.ConstantData: return this.rdataSection;
                case PESectionKind.CoverageData: return this.coverSection;
                case PESectionKind.StaticData: return this.sdataSection;
                case PESectionKind.ThreadLocalStorage: return this.tlsSection;
                default: return this.textDataSection;
            }
        }

        private StringIdx GetStringIndex(string str)
        {
            Debug.Assert(str.Length == 0 || MetadataHelpers.IsValidMetadataIdentifier(str));

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
            Location location = Location.None;
            ISymbol symbol = errorEntity as ISymbol;
            if (symbol != null && !symbol.Locations.IsDefaultOrEmpty)
            {
                location = symbol.Locations[0];
            }
            return location;
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
            result = this.GetBlobIndex(sig.ToArray());
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

        private uint MetadataHeaderSize
        {
            get { return (uint)(this.IsMinimalDelta ? 124 : 108); }
        }

        private void SerializeMetadataHeader(BinaryWriter writer, uint tableStreamLength)
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
            uint offsetFromStartOfMetadata = this.MetadataHeaderSize;
            SerializeStreamHeader(ref offsetFromStartOfMetadata, tableStreamLength, (this.CompressMetadataStream ? "#~" : "#-"), writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, this.stringWriter.BaseStream.Length, "#Strings", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, this.userStringWriter.BaseStream.Length, "#US", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, this.guidWriter.BaseStream.Length, "#GUID", writer);
            SerializeStreamHeader(ref offsetFromStartOfMetadata, this.blobWriter.BaseStream.Length, "#Blob", writer);
            if (this.IsMinimalDelta)
            {
                SerializeStreamHeader(ref offsetFromStartOfMetadata, 0, "#JTD", writer);
            }

            uint endOffset = writer.BaseStream.Position;
            Debug.Assert(endOffset - startOffset == MetadataHeaderSize);
        }

        private static void SerializeStreamHeader(ref uint offsetFromStartOfMetadata, uint sizeOfStreamHeap, string streamName, BinaryWriter writer)
        {
            // 4 for the first uint (offset), 4 for the second uint (padded size), length of stream name + 1 for null terminator (then padded)
            uint sizeOfStreamHeader = 8 + Aligned((uint)streamName.Length + 1, 4);
            writer.WriteUint(offsetFromStartOfMetadata);
            writer.WriteUint(Aligned(sizeOfStreamHeap, 4));
            foreach (char ch in streamName)
            {
                writer.WriteByte((byte)ch);
            }

            // After offset, size, and stream name, write 0-bytes until we reach our padded size.
            for (uint i = 8 + (uint)streamName.Length; i < sizeOfStreamHeader; i++)
            {
                writer.WriteByte(0);
            }

            offsetFromStartOfMetadata += sizeOfStreamHeap;
        }

        private void SerializeMetadata(BinaryWriter metadataWriter, bool separateMethodIL, out uint moduleVersionIdOffset)
        {
            uint metadataStartOffset = metadataWriter.BaseStream.Position;
            uint metadataTablesStartOffset = metadataStartOffset + MetadataHeaderSize;

            // Leave space for the metadata header. We need to fill in the sizes of all tables and heaps.
            // It's easier to write it at the end then to precalculate the sizes.
            metadataWriter.BaseStream.Position = metadataTablesStartOffset;
            this.SerializeMetadataTables(metadataWriter, separateMethodIL);

            uint metadataTablesEndOffset = metadataWriter.BaseStream.Position;

            this.stringWriter.BaseStream.WriteTo(metadataWriter.BaseStream);
            this.userStringWriter.BaseStream.WriteTo(metadataWriter.BaseStream);

            uint guidHeapStartOffset = metadataWriter.BaseStream.Position;
            moduleVersionIdOffset = GetModuleVersionGuidOffsetInMetadataStream(guidHeapStartOffset);
            
            this.guidWriter.BaseStream.WriteTo(metadataWriter.BaseStream);
            this.blobWriter.BaseStream.WriteTo(metadataWriter.BaseStream);

            uint metadataSize = metadataWriter.BaseStream.Position;

            // write header at the start of the metadata stream:
            metadataWriter.BaseStream.Position = 0;
            this.SerializeMetadataHeader(metadataWriter, tableStreamLength: metadataTablesEndOffset - metadataTablesStartOffset);

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

        private void SerializeMetadataTables(BinaryWriter writer, bool separateMethodIL)
        {
            this.SerializeTablesHeader(writer);
            this.SerializeModuleTable(writer);
            this.SerializeTypeRefTable(writer);
            this.SerializeTypeDefTable(writer);
            this.SerializeFieldTable(writer);
            this.SerializeMethodTable(writer, separateMethodIL);
            this.SerializeParamTable(writer);
            this.SerializeInterfaceImplTable(writer);
            this.SerializeMemberRefTable(writer);
            this.SerializeConstantTable(writer);
            this.SerializeCustomAttributeTable(writer);
            this.SerializeFieldMarshalTable(writer);
            this.SerializeDeclSecurityTable(writer);
            this.SerializeClassLayoutTable(writer);
            this.SerializeFieldLayoutTable(writer);
            this.SerializeStandAloneSigTable(writer);
            this.SerializeEventMapTable(writer);
            this.SerializeEventTable(writer);
            this.SerializePropertyMapTable(writer);
            this.SerializePropertyTable(writer);
            this.SerializeMethodSemanticsTable(writer);
            this.SerializeMethodImplTable(writer);
            this.SerializeModuleRefTable(writer);
            this.SerializeTypeSpecTable(writer);
            this.SerializeImplMapTable(writer);
            this.SerializeFieldRvaTable(writer);
            this.SerializeEncLogTable(writer);
            this.SerializeEncMapTable(writer);
            this.SerializeAssemblyTable(writer);
            this.SerializeAssemblyRefTable(writer);
            this.SerializeFileTable(writer);
            this.SerializeExportedTypeTable(writer);
            this.SerializeManifestResourceTable(writer);
            this.SerializeNestedClassTable(writer);
            this.SerializeGenericParamTable(writer);
            this.SerializeMethodSpecTable(writer);
            this.SerializeGenericParamConstraintTable(writer);
            writer.WriteByte(0);
            writer.Align(4);
        }

        private void ComputeColumnSizes()
        {
            const byte large = 4;
            const byte small = 2;

            // TODO (tomat): reuse values from metadata reader?

            this.blobIndexSize = (IsMinimalDelta || this.blobWriter.BaseStream.Length > ushort.MaxValue) ? large : small;
            this.stringIndexSize = (IsMinimalDelta || this.stringWriter.BaseStream.Length > ushort.MaxValue) ? large : small;
            this.guidIndexSize = (IsMinimalDelta || this.guidWriter.BaseStream.Length > ushort.MaxValue) ? large : small;

            this.customAttributeTypeCodedIndexSize = this.GetIndexByteSize(3, TableIndex.MethodDef, TableIndex.MemberRef);
            this.declSecurityCodedIndexSize = this.GetIndexByteSize(2, TableIndex.MethodDef, TableIndex.TypeDef);
            this.eventDefIndexSize = this.GetIndexByteSize(0, TableIndex.Event);
            this.fieldDefIndexSize = this.GetIndexByteSize(0, TableIndex.Field);
            this.genericParamIndexSize = this.GetIndexByteSize(0, TableIndex.GenericParam);
            this.hasConstantCodedIndexSize = this.GetIndexByteSize(2, TableIndex.Field, TableIndex.Param, TableIndex.Property);

            this.hasCustomAttributeCodedIndexSize = this.GetIndexByteSize(5,
                TableIndex.MethodDef,
                TableIndex.Field,
                TableIndex.TypeRef,
                TableIndex.TypeDef,
                TableIndex.Param,
                TableIndex.InterfaceImpl,
                TableIndex.MemberRef,
                TableIndex.Module,
                TableIndex.DeclSecurity,
                TableIndex.Property,
                TableIndex.Event,
                TableIndex.StandAloneSig,
                TableIndex.ModuleRef,
                TableIndex.TypeSpec,
                TableIndex.Assembly,
                TableIndex.AssemblyRef,
                TableIndex.File,
                TableIndex.ExportedType,
                TableIndex.ManifestResource,
                TableIndex.GenericParam,
                TableIndex.GenericParamConstraint,
                TableIndex.MethodSpec);

            this.hasFieldMarshalCodedIndexSize = this.GetIndexByteSize(1, TableIndex.Field, TableIndex.Param);
            this.hasSemanticsCodedIndexSize = this.GetIndexByteSize(1, TableIndex.Event, TableIndex.Property);
            this.implementationCodedIndexSize = this.GetIndexByteSize(2, TableIndex.File, TableIndex.AssemblyRef, TableIndex.ExportedType);
            this.memberForwardedCodedIndexSize = this.GetIndexByteSize(1, TableIndex.Field, TableIndex.MethodDef);
            this.memberRefParentCodedIndexSize = this.GetIndexByteSize(3, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.ModuleRef, TableIndex.MethodDef, TableIndex.TypeSpec);
            this.methodDefIndexSize = this.GetIndexByteSize(0, TableIndex.MethodDef);
            this.methodDefOrRefCodedIndexSize = this.GetIndexByteSize(1, TableIndex.MethodDef, TableIndex.MemberRef);
            this.moduleRefIndexSize = this.GetIndexByteSize(0, TableIndex.ModuleRef);
            this.parameterIndexSize = this.GetIndexByteSize(0, TableIndex.Param);
            this.propertyDefIndexSize = this.GetIndexByteSize(0, TableIndex.Property);
            this.resolutionScopeCodedIndexSize = this.GetIndexByteSize(2, TableIndex.Module, TableIndex.ModuleRef, TableIndex.AssemblyRef, TableIndex.TypeRef);
            this.typeDefIndexSize = this.GetIndexByteSize(0, TableIndex.TypeDef);
            this.typeDefOrRefCodedIndexSize = this.GetIndexByteSize(2, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.TypeSpec);
            this.typeOrMethodDefCodedIndexSize = this.GetIndexByteSize(1, TableIndex.TypeDef, TableIndex.MethodDef);
        }

        private byte GetIndexByteSize(int discriminatingBits, params TableIndex[] tables)
        {
            const int BitsPerShort = 16;
            return (byte)(IsMinimalDelta || IndexDoesNotFit(BitsPerShort - discriminatingBits, tables) ? 4 : 2);
        }

        private bool IndexDoesNotFit(int numberOfBits, params TableIndex[] tables)
        {
            uint maxIndex = (uint)(1 << numberOfBits) - 1;
            foreach (TableIndex table in tables)
            {
                if (this.tableSizes[(uint)table] > maxIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private void SerializeMetadataAndIL(BinaryWriter metadataWriter, BinaryWriter ilWriter, bool separateMethodIL, out uint moduleVersionIdOffsetInMetadataStream)
        {
            uint[] methodBodyRvas = SerializeMethodBodies(ilWriter);

            cancellationToken.ThrowIfCancellationRequested();

            // method body serialization adds Stand Alone Signatures
            this.tableIndicesAreComplete = true;

            PopulateTables(methodBodyRvas);

            // Do this as soon as table rows are done and before we need to final size of string table
            SerializeStringHeap();

            // Do this here so that tables and win32 resources can contain actual RVAs without the need for fixups.
            FillInSectionHeaders((int)ilWriter.BaseStream.Length);

            // Align heaps
            this.OnBeforeHeapsAligned();
            this.stringWriter.Align(4);
            this.userStringWriter.Align(4);
            this.guidWriter.Align(4);
            this.blobWriter.Align(4);

            SerializeMetadata(metadataWriter, separateMethodIL, moduleVersionIdOffset: out moduleVersionIdOffsetInMetadataStream);
        }

        private void PopulateTables(uint[] methodBodyRvas)
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
            this.PopulateFieldRvaTableRows();
            this.PopulateFieldTableRows();
            this.PopulateFileTableRows();
            this.PopulateGenericParamTableRows();
            this.PopulateGenericParamConstraintTableRows();
            this.PopulateImplMapTableRows();
            this.PopulateInterfaceImplTableRows();
            this.PopulateManifestResourceTableRows();
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
            this.PopulateStandAloneSigTableRows();
            this.PopulateTypeDefTableRows();
            this.PopulateTypeRefTableRows();
            this.PopulateTypeSpecTableRows();

            // This table is populated after the others because it depends on the order of the entries of the generic parameter table.
            this.PopulateCustomAttributeTableRows();

            this.PopulateEncLogTableRows();
            this.PopulateEncMapRows();
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
                r.Version = assemblyRef.Version ?? NullVersion;
                if (IteratorHelper.EnumerableIsNotEmpty(assemblyRef.PublicKeyToken))
                {
                    r.PublicKeyToken = this.GetBlobIndex(new List<byte>(assemblyRef.PublicKeyToken).ToArray());
                }

                Debug.Assert(!string.IsNullOrEmpty(assemblyRef.Name));
                r.Name = this.GetStringIndexForPathAndCheckLength(assemblyRef.Name, assemblyRef);

                if (assemblyRef.Culture != null)
                {
                    r.Culture = this.GetStringIndex(assemblyRef.Culture);
                }

                r.IsRetargetable = assemblyRef.IsRetargetable;
                r.ContentType = assemblyRef.ContentType;
                this.assemblyRefTable.Add(r);
            }

            this.tableSizes[(uint)TableIndex.AssemblyRef] = (uint)this.assemblyRefTable.Count;
        }

        private static readonly Version NullVersion = new Version(0, 0, 0, 0);

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
                    (x.Version ?? NullVersion).Equals(y.Version ?? NullVersion) &&
                    ByteSequenceComparer.Instance.Equals(
                        x.PublicKeyToken ?? SpecializedCollections.EmptyBytes,
                        y.PublicKeyToken ?? SpecializedCollections.EmptyBytes) &&
                    x.Name == y.Name &&
                    (x.Culture ?? String.Empty) == (y.Culture ?? String.Empty);
            }

            public int GetHashCode(IAssemblyReference reference)
            {
                return
                    Hash.Combine(reference.Version ?? NullVersion,
                      Hash.Combine(ByteSequenceComparer.Instance.GetHashCode(reference.PublicKeyToken ?? SpecializedCollections.EmptyBytes),
                          Hash.Combine(reference.Name.GetHashCode(), (reference.Culture ?? String.Empty).GetHashCode()
                          )));
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

            //EDMAURER make sure that GetBlobIndex overload that takes byte[] is called.
            this.assemblyKey = (IteratorHelper.EnumerableIsNotEmpty(assembly.PublicKey)) ? this.GetBlobIndex(new List<byte>(assembly.PublicKey).ToArray()) : 0;
            this.assemblyName = this.GetStringIndexForPathAndCheckLength(assembly.Name, assembly);
            this.assemblyCulture = (assembly.Culture != null) ? this.GetStringIndex(assembly.Culture) : StringIdx.Empty;
            this.tableSizes[(uint)TableIndex.Assembly] = 1;
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

            this.tableSizes[(uint)TableIndex.ClassLayout] = (uint)this.classLayoutTable.Count;
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

            this.tableSizes[(uint)TableIndex.Constant] = (uint)this.constantTable.Count;
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
                Value = this.GetBlobIndex(value)
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

            // The indices of this.genericParameterList do not correspond to the table indices because the
            // the table may be sorted after the list has been constructed.
            // Note that in all other cases, tables that are sorted are sorted in an order that depends
            // only on list indices. The generic parameter table is the sole exception.
            List<IGenericParameter> sortedGenericParameterList = new List<IGenericParameter>();
            foreach (GenericParamRow genericParamRow in this.genericParamTable)
            {
                sortedGenericParameterList.Add(genericParamRow.GenericParameter);
            }

            this.AddCustomAttributesToTable(sortedGenericParameterList, 19, this.GetGenericParameterIndex);

            this.customAttributeTable.Sort(new CustomAttributeRowComparer());
            this.tableSizes[(uint)TableIndex.CustomAttribute] = (uint)this.customAttributeTable.Count;
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
                this.tableSizes[(uint)TableIndex.TypeRef] = (uint)this.typeRefTable.Count;
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

        private void AddCustomAttributesToTable(IEnumerable<IReference> parentList, uint tag)
        {
            uint parentIndex = 0;
            foreach (IReference parent in parentList)
            {
                Debug.Assert(this.IsFullMetadata); // parentToken is not relative
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
            this.tableSizes[(uint)TableIndex.DeclSecurity] = (uint)this.declSecurityTable.Count;
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
                int result = ((int)x.Parent) - (int)y.Parent;
                if (result == 0)
                {
                    result = x.OriginalIndex - y.OriginalIndex;
                }

                return result;
            }
        }

        private struct DeclSecurityRow { public ushort Action; public uint Parent; public uint PermissionSet; public int OriginalIndex; }

        private readonly List<DeclSecurityRow> declSecurityTable = new List<DeclSecurityRow>();

        private void PopulateEncLogTableRows()
        {
            this.PopulateEncLogTableRows(this.encLogTable);
            this.tableSizes[(uint)TableIndex.EncLog] = (uint)this.encLogTable.Count;
        }

        protected struct EncLogRow { public uint Token; public EncFuncCode FuncCode; }

        private readonly List<EncLogRow> encLogTable = new List<EncLogRow>();

        private void PopulateEncMapRows()
        {
            this.PopulateEncMapTableRows(this.encMapTable);
            this.tableSizes[(uint)TableIndex.EncMap] = (uint)this.encMapTable.Count;
        }

        protected struct EncMapRow { public uint Token; }

        private readonly List<EncMapRow> encMapTable = new List<EncMapRow>();

        private void PopulateEventMapTableRows()
        {
            this.PopulateEventMapTableRows(this.eventMapTable);
            this.tableSizes[(uint)TableIndex.EventMap] = (uint)this.eventMapTable.Count;
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

            this.tableSizes[(uint)TableIndex.Event] = (uint)this.eventTable.Count;
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

            this.tableSizes[(uint)TableIndex.ExportedType] = (uint)this.exportedTypeTable.Count;
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

            this.tableSizes[(uint)TableIndex.FieldLayout] = (uint)this.fieldLayoutTable.Count;
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

            this.tableSizes[(uint)TableIndex.FieldMarshal] = (uint)this.fieldMarshalTable.Count;
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

        private void PopulateFieldRvaTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (!fieldDef.IsMapped)
                {
                    continue;
                }

                uint fieldIndex = this.GetFieldDefIndex(fieldDef);
                FieldRvaRow r = new FieldRvaRow();
                r.SectionKind = fieldDef.FieldMapping.PESectionKind;
                r.Offset = this.GetDataOffset(fieldDef.FieldMapping);
                r.Field = fieldIndex;
                this.fieldRvaTable.Add(r);
            }

            this.tableSizes[(uint)TableIndex.FieldRva] = (uint)this.fieldRvaTable.Count;
        }

        private struct FieldRvaRow { public PESectionKind SectionKind; public uint Offset; public uint Field; }

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
                    ((IContextualNamedEntity)fieldDef).AssociateWithPeWriter(this);
                }

                r.Name = this.GetStringIndexForNameAndCheckLength(fieldDef.Name, fieldDef);
                r.Signature = this.GetFieldSignatureIndex(fieldDef);
                this.fieldDefTable.Add(r);
            }

            this.tableSizes[(uint)TableIndex.Field] = (uint)this.fieldDefTable.Count;
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
                r.HashValue = this.GetBlobIndex(fileReference.GetHashValue(hashAlgorithm).ToArray());
                this.fileTable.Add(r);
            }

            this.tableSizes[(uint)TableIndex.File] = (uint)this.fileTable.Count;
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

            this.tableSizes[(uint)TableIndex.GenericParamConstraint] = (uint)this.genericParamConstraintTable.Count;
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
            this.tableSizes[(uint)TableIndex.GenericParam] = (uint)this.genericParamTable.Count;
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

            this.tableSizes[(uint)TableIndex.ImplMap] = (uint)this.implMapTable.Count;
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

            this.tableSizes[(uint)TableIndex.InterfaceImpl] = (uint)this.interfaceImplTable.Count;
        }

        private struct InterfaceImplRow { public uint Class; public uint Interface; }

        private readonly List<InterfaceImplRow> interfaceImplTable = new List<InterfaceImplRow>();

        private void PopulateManifestResourceTableRows()
        {
            foreach (var resource in this.module.GetResources(Context))
            {
                ManifestResourceRow r = new ManifestResourceRow();
                r.Offset = this.GetManagedResourceOffset(resource);
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

            this.tableSizes[(uint)TableIndex.ManifestResource] = (uint)this.manifestResourceTable.Count;
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

            this.tableSizes[(uint)TableIndex.MemberRef] = (uint)this.memberRefTable.Count;
        }

        private struct MemberRefRow { public uint Class; public StringIdx Name; public uint Signature; }

        private readonly List<MemberRefRow> memberRefTable = new List<MemberRefRow>();

        private void PopulateMethodImplTableRows()
        {
            this.methodImplTable.Capacity = this.methodImplList.Count;

            foreach (IMethodImplementation methodImplementation in this.methodImplList)
            {
                MethodImplRow r = new MethodImplRow();
                r.Class = this.GetTypeDefIndex(methodImplementation.ContainingType);
                r.MethodBody = this.GetMethodDefOrRefCodedIndex(methodImplementation.ImplementingMethod);
                r.MethodDecl = this.GetMethodDefOrRefCodedIndex(methodImplementation.ImplementedMethod);
                this.methodImplTable.Add(r);
            }

            this.tableSizes[(uint)TableIndex.MethodImpl] = (uint)this.methodImplTable.Count;
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

            this.tableSizes[(uint)TableIndex.MethodSemantics] = (uint)this.methodSemanticsTable.Count;
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

            this.tableSizes[(uint)TableIndex.MethodSpec] = (uint)this.methodSpecTable.Count;
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

            this.tableSizes[(uint)TableIndex.MethodDef] = (uint)this.methodTable.Length;
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

            this.tableSizes[(uint)TableIndex.ModuleRef] = (uint)this.moduleRefTable.Count;
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
            this.tableSizes[(uint)TableIndex.Module] = 1;
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

            this.tableSizes[(uint)TableIndex.NestedClass] = (uint)this.nestedClassTable.Count;
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

            this.tableSizes[(uint)TableIndex.Param] = (uint)this.paramTable.Count;
        }

        private struct ParamRow { public ushort Flags; public ushort Sequence; public StringIdx Name; }

        private readonly List<ParamRow> paramTable = new List<ParamRow>();

        private void PopulatePropertyMapTableRows()
        {
            this.PopulatePropertyMapTableRows(this.propertyMapTable);
            this.tableSizes[(uint)TableIndex.PropertyMap] = (uint)this.propertyMapTable.Count;
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

            this.tableSizes[(uint)TableIndex.Property] = (uint)this.propertyTable.Count;
        }

        private struct PropertyRow { public ushort PropFlags; public StringIdx Name; public uint Type; }

        private readonly List<PropertyRow> propertyTable = new List<PropertyRow>();

        private void PopulateStandAloneSigTableRows()
        {
            this.tableSizes[(uint)TableIndex.StandAloneSig] = (uint)this.GetStandAloneSignatures().Count;
        }

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

            this.tableSizes[(uint)TableIndex.TypeDef] = (uint)this.typeDefTable.Count;
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

            this.tableSizes[(uint)TableIndex.TypeRef] = (uint)this.typeRefTable.Count;
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

            this.tableSizes[(uint)TableIndex.TypeSpec] = (uint)this.typeSpecTable.Count;
        }

        private struct TypeSpecRow { public uint Signature; }

        private readonly List<TypeSpecRow> typeSpecTable = new List<TypeSpecRow>();

        private void SerializeTablesHeader(BinaryWriter writer)
        {
            HeapSizeFlag heapSizes = 0;
            if (this.stringIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.StringHeapLarge;
            }

            if (this.guidIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.GuidHeapLarge;
            }

            if (this.blobIndexSize > 2)
            {
                heapSizes |= HeapSizeFlag.BlobHeapLarge;
            }

            if (!this.IsFullMetadata)
            {
                heapSizes |= (HeapSizeFlag.EnCDeltas | HeapSizeFlag.DeletedMarks);
            }

            ulong validTables = 0;
            ulong sortedTables = 0;
            this.ComputeValidAndSortedMasks(out validTables, out sortedTables);

            writer.WriteUint(0); // reserved
            writer.WriteByte(this.module.MetadataFormatMajorVersion);
            writer.WriteByte(this.module.MetadataFormatMinorVersion);
            writer.WriteByte((byte)heapSizes);
            writer.WriteByte(1); // reserved
            writer.WriteUlong(validTables);
            writer.WriteUlong(sortedTables);
            this.SerializeTableSizes(writer);
        }

        internal void GetTableSizes(int[] sizes)
        {
            for (int i = 0; i < this.tableSizes.Length; i++)
            {
                sizes[i] = (int)this.tableSizes[i];
            }
        }

        private uint ComputeSizeOfTablesHeader()
        {
            uint result = 4 + 4 + 8 + 8;
            foreach (uint tableSize in this.tableSizes)
            {
                if (tableSize > 0)
                {
                    result += 4;
                }
            }

            return result;
        }

        private void ComputeValidAndSortedMasks(out ulong validTables, out ulong sortedTables)
        {
            validTables = 0;
            ulong validBit = 1;

            foreach (uint tableSize in this.tableSizes)
            {
                if (tableSize > 0)
                {
                    validTables |= validBit;
                }

                validBit <<= 1;
            }

            sortedTables = 0x16003301fa00/* & validTables*/;
        }

        private void SerializeTableSizes(BinaryWriter writer)
        {
            foreach (uint tableSize in this.tableSizes)
            {
                if (tableSize > 0)
                {
                    writer.WriteUint(tableSize);
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

        private void SerializeModuleTable(BinaryWriter writer)
        {
            writer.WriteUshort(this.moduleRow.Generation); // generation (Edit & Continue)
            SerializeIndex(writer, this.moduleRow.Name, this.stringIndexSize);
            SerializeIndex(writer, this.moduleRow.ModuleVersionId, this.guidIndexSize); // module version id GUID index
            SerializeIndex(writer, this.moduleRow.EncId, this.guidIndexSize); // Edit & Continue Id GUID
            SerializeIndex(writer, this.moduleRow.EncBaseId, this.guidIndexSize); // Edit & Continue Base Id GUID
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

        private void SerializeTypeRefTable(BinaryWriter writer)
        {
            foreach (TypeRefRow typeRef in this.typeRefTable)
            {
                SerializeIndex(writer, typeRef.ResolutionScope, this.resolutionScopeCodedIndexSize);
                this.SerializeIndex(writer, typeRef.Name, this.stringIndexSize);
                this.SerializeIndex(writer, typeRef.Namespace, this.stringIndexSize);
            }
        }

        private void SerializeTypeDefTable(BinaryWriter writer)
        {
            foreach (TypeDefRow typeDef in this.typeDefTable)
            {
                writer.WriteUint(typeDef.Flags);
                this.SerializeIndex(writer, typeDef.Name, this.stringIndexSize);
                this.SerializeIndex(writer, typeDef.Namespace, this.stringIndexSize);
                SerializeIndex(writer, typeDef.Extends, this.typeDefOrRefCodedIndexSize);
                SerializeIndex(writer, typeDef.FieldList, this.fieldDefIndexSize);
                SerializeIndex(writer, typeDef.MethodList, this.methodDefIndexSize);
            }
        }

        private void SerializeFieldTable(BinaryWriter writer)
        {
            foreach (FieldDefRow fieldDef in this.fieldDefTable)
            {
                writer.WriteUshort(fieldDef.Flags);
                this.SerializeIndex(writer, fieldDef.Name, this.stringIndexSize);
                SerializeIndex(writer, fieldDef.Signature, this.blobIndexSize);
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

        private void SerializeMethodTable(BinaryWriter writer, bool separateMethodIL)
        {
            foreach (MethodRow method in this.methodTable)
            {
                if (method.Rva == uint.MaxValue)
                {
                    writer.WriteUint(0);
                }
                else if (separateMethodIL)
                {
                    // RVA is relative to the start of IL stream 
                    // if the stream is not part of the PE image
                    writer.WriteUint(method.Rva);
                }
                else
                {
                    writer.WriteUint(GetRva(this.textMethodBodySection, method.Rva));
                }

                writer.WriteUshort(method.ImplFlags);
                writer.WriteUshort(method.Flags);
                this.SerializeIndex(writer, method.Name, this.stringIndexSize);
                SerializeIndex(writer, method.Signature, this.blobIndexSize);
                SerializeIndex(writer, method.ParamList, this.parameterIndexSize);
            }
        }

        private void SerializeParamTable(BinaryWriter writer)
        {
            foreach (ParamRow param in this.paramTable)
            {
                writer.WriteUshort(param.Flags);
                writer.WriteUshort(param.Sequence);
                this.SerializeIndex(writer, param.Name, this.stringIndexSize);
            }
        }

        private void SerializeInterfaceImplTable(BinaryWriter writer)
        {
            foreach (InterfaceImplRow interfaceImpl in this.interfaceImplTable)
            {
                SerializeIndex(writer, interfaceImpl.Class, this.typeDefIndexSize);
                SerializeIndex(writer, interfaceImpl.Interface, this.typeDefOrRefCodedIndexSize);
            }
        }

        private void SerializeMemberRefTable(BinaryWriter writer)
        {
            foreach (MemberRefRow memberRef in this.memberRefTable)
            {
                SerializeIndex(writer, memberRef.Class, this.memberRefParentCodedIndexSize);
                SerializeIndex(writer, memberRef.Name, this.stringIndexSize);
                SerializeIndex(writer, memberRef.Signature, this.blobIndexSize);
            }
        }

        private void SerializeConstantTable(BinaryWriter writer)
        {
            foreach (ConstantRow constant in this.constantTable)
            {
                writer.WriteByte(constant.Type);
                writer.WriteByte(0);
                SerializeIndex(writer, constant.Parent, this.hasConstantCodedIndexSize);
                SerializeIndex(writer, constant.Value, this.blobIndexSize);
            }
        }

        private void SerializeCustomAttributeTable(BinaryWriter writer)
        {
            foreach (CustomAttributeRow customAttribute in this.customAttributeTable)
            {
                SerializeIndex(writer, customAttribute.Parent, this.hasCustomAttributeCodedIndexSize);
                SerializeIndex(writer, customAttribute.Type, this.customAttributeTypeCodedIndexSize);
                SerializeIndex(writer, customAttribute.Value, this.blobIndexSize);
            }
        }

        private void SerializeFieldMarshalTable(BinaryWriter writer)
        {
            foreach (FieldMarshalRow fieldMarshal in this.fieldMarshalTable)
            {
                SerializeIndex(writer, fieldMarshal.Parent, this.hasFieldMarshalCodedIndexSize);
                SerializeIndex(writer, fieldMarshal.NativeType, this.blobIndexSize);
            }
        }

        private void SerializeDeclSecurityTable(BinaryWriter writer)
        {
            foreach (DeclSecurityRow declSecurity in this.declSecurityTable)
            {
                writer.WriteUshort(declSecurity.Action);
                SerializeIndex(writer, declSecurity.Parent, this.declSecurityCodedIndexSize);
                SerializeIndex(writer, declSecurity.PermissionSet, this.blobIndexSize);
            }
        }

        private void SerializeClassLayoutTable(BinaryWriter writer)
        {
            foreach (ClassLayoutRow classLayout in this.classLayoutTable)
            {
                writer.WriteUshort(classLayout.PackingSize);
                writer.WriteUint(classLayout.ClassSize);
                SerializeIndex(writer, classLayout.Parent, this.typeDefIndexSize);
            }
        }

        private void SerializeFieldLayoutTable(BinaryWriter writer)
        {
            foreach (FieldLayoutRow fieldLayout in this.fieldLayoutTable)
            {
                writer.WriteUint(fieldLayout.Offset);
                SerializeIndex(writer, fieldLayout.Field, this.fieldDefIndexSize);
            }
        }

        private void SerializeStandAloneSigTable(BinaryWriter writer)
        {
            foreach (uint blobIndex in this.GetStandAloneSignatures())
            {
                SerializeIndex(writer, blobIndex, this.blobIndexSize);
            }
        }

        private void SerializeEventMapTable(BinaryWriter writer)
        {
            foreach (EventMapRow eventMap in this.eventMapTable)
            {
                SerializeIndex(writer, eventMap.Parent, this.typeDefIndexSize);
                SerializeIndex(writer, eventMap.EventList, this.eventDefIndexSize);
            }
        }

        private void SerializeEventTable(BinaryWriter writer)
        {
            foreach (EventRow eventRow in this.eventTable)
            {
                writer.WriteUshort(eventRow.EventFlags);
                SerializeIndex(writer, eventRow.Name, this.stringIndexSize);
                SerializeIndex(writer, eventRow.EventType, this.typeDefOrRefCodedIndexSize);
            }
        }

        private void SerializePropertyMapTable(BinaryWriter writer)
        {
            foreach (PropertyMapRow propertyMap in this.propertyMapTable)
            {
                SerializeIndex(writer, propertyMap.Parent, this.typeDefIndexSize);
                SerializeIndex(writer, propertyMap.PropertyList, this.propertyDefIndexSize);
            }
        }

        private void SerializePropertyTable(BinaryWriter writer)
        {
            foreach (PropertyRow property in this.propertyTable)
            {
                writer.WriteUshort(property.PropFlags);
                this.SerializeIndex(writer, property.Name, this.stringIndexSize);
                SerializeIndex(writer, property.Type, this.blobIndexSize);
            }
        }

        private void SerializeMethodSemanticsTable(BinaryWriter writer)
        {
            foreach (MethodSemanticsRow methodSemantic in this.methodSemanticsTable)
            {
                writer.WriteUshort(methodSemantic.Semantic);
                SerializeIndex(writer, methodSemantic.Method, this.methodDefIndexSize);
                SerializeIndex(writer, methodSemantic.Association, this.hasSemanticsCodedIndexSize);
            }
        }

        private void SerializeMethodImplTable(BinaryWriter writer)
        {
            foreach (MethodImplRow methodImpl in this.methodImplTable)
            {
                SerializeIndex(writer, methodImpl.Class, this.typeDefIndexSize);
                SerializeIndex(writer, methodImpl.MethodBody, this.methodDefOrRefCodedIndexSize);
                SerializeIndex(writer, methodImpl.MethodDecl, this.methodDefOrRefCodedIndexSize);
            }
        }

        private void SerializeModuleRefTable(BinaryWriter writer)
        {
            foreach (ModuleRefRow moduleRef in this.moduleRefTable)
            {
                this.SerializeIndex(writer, moduleRef.Name, this.stringIndexSize);
            }
        }

        private void SerializeTypeSpecTable(BinaryWriter writer)
        {
            foreach (TypeSpecRow typeSpec in this.typeSpecTable)
            {
                SerializeIndex(writer, typeSpec.Signature, this.blobIndexSize);
            }
        }

        private void SerializeImplMapTable(BinaryWriter writer)
        {
            foreach (ImplMapRow implMap in this.implMapTable)
            {
                writer.WriteUshort(implMap.MappingFlags);
                SerializeIndex(writer, implMap.MemberForwarded, this.memberForwardedCodedIndexSize);
                this.SerializeIndex(writer, implMap.ImportName, this.stringIndexSize);
                SerializeIndex(writer, implMap.ImportScope, this.moduleRefIndexSize);
            }
        }

        private void SerializeFieldRvaTable(BinaryWriter writer)
        {
            foreach (FieldRvaRow fieldRva in this.fieldRvaTable)
            {
                writer.WriteUint(GetRva(this.GetSection(fieldRva.SectionKind), fieldRva.Offset));
                SerializeIndex(writer, fieldRva.Field, this.fieldDefIndexSize);
            }
        }

        private void SerializeAssemblyTable(BinaryWriter writer)
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
            SerializeIndex(writer, this.assemblyKey, this.blobIndexSize);
            this.SerializeIndex(writer, this.assemblyName, this.stringIndexSize);
            this.SerializeIndex(writer, this.assemblyCulture, this.stringIndexSize);
        }

        private void SerializeAssemblyRefTable(BinaryWriter writer)
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

                SerializeIndex(writer, assemblyRef.PublicKeyToken, this.blobIndexSize);
                this.SerializeIndex(writer, assemblyRef.Name, this.stringIndexSize);
                this.SerializeIndex(writer, assemblyRef.Culture, this.stringIndexSize);
                SerializeIndex(writer, 0, this.blobIndexSize); // hash of referenced assembly. Omitted.
            }
        }

        private void SerializeFileTable(BinaryWriter writer)
        {
            foreach (FileTableRow fileReference in this.fileTable)
            {
                writer.WriteUint(fileReference.Flags);
                this.SerializeIndex(writer, fileReference.FileName, this.stringIndexSize);
                SerializeIndex(writer, fileReference.HashValue, this.blobIndexSize);
            }
        }

        private void SerializeExportedTypeTable(BinaryWriter writer)
        {
            foreach (ExportedTypeRow exportedType in this.exportedTypeTable)
            {
                writer.WriteUint((uint)exportedType.Flags);
                writer.WriteUint(exportedType.TypeDefId);
                this.SerializeIndex(writer, exportedType.TypeName, this.stringIndexSize);
                this.SerializeIndex(writer, exportedType.TypeNamespace, this.stringIndexSize);
                SerializeIndex(writer, exportedType.Implementation, this.implementationCodedIndexSize);
            }
        }

        private void SerializeManifestResourceTable(BinaryWriter writer)
        {
            foreach (ManifestResourceRow manifestResource in this.manifestResourceTable)
            {
                writer.WriteUint(manifestResource.Offset);
                writer.WriteUint(manifestResource.Flags);
                this.SerializeIndex(writer, manifestResource.Name, this.stringIndexSize);
                SerializeIndex(writer, manifestResource.Implementation, this.implementationCodedIndexSize);
            }
        }

        private void SerializeNestedClassTable(BinaryWriter writer)
        {
            foreach (NestedClassRow nestedClass in this.nestedClassTable)
            {
                SerializeIndex(writer, nestedClass.NestedClass, this.typeDefIndexSize);
                SerializeIndex(writer, nestedClass.EnclosingClass, this.typeDefIndexSize);
            }
        }

        private void SerializeGenericParamTable(BinaryWriter writer)
        {
            foreach (GenericParamRow genericParam in this.genericParamTable)
            {
                writer.WriteUshort(genericParam.Number);
                writer.WriteUshort(genericParam.Flags);
                SerializeIndex(writer, genericParam.Owner, this.typeOrMethodDefCodedIndexSize);
                this.SerializeIndex(writer, genericParam.Name, this.stringIndexSize);
            }
        }

        private void SerializeMethodSpecTable(BinaryWriter writer)
        {
            foreach (MethodSpecRow methodSpec in this.methodSpecTable)
            {
                SerializeIndex(writer, methodSpec.Method, this.methodDefOrRefCodedIndexSize);
                SerializeIndex(writer, methodSpec.Instantiation, this.blobIndexSize);
            }
        }

        private void SerializeGenericParamConstraintTable(BinaryWriter writer)
        {
            foreach (GenericParamConstraintRow genericParamConstraint in this.genericParamConstraintTable)
            {
                SerializeIndex(writer, genericParamConstraint.Owner, this.genericParamIndexSize);
                SerializeIndex(writer, genericParamConstraint.Constraint, this.typeDefOrRefCodedIndexSize);
            }
        }
        
        private uint[] SerializeMethodBodies(BinaryWriter writer)
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
                        uint localSignatureToken = this.SerializeLocalVariablesSignature(body.LocalVariables);

                        // TODO: consider parallelizing these (local signature tokens can be piped into IL serialization & debug info generation)
                        rva = this.SerializeMethodBody(body, writer, localSignatureToken);

                        if (pdbWriter != null)
                        {
                            pdbWriter.SerializeDebugInfo(body, localSignatureToken, customDebugInfoWriter);
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
            uint bodyRva = 0;

            byte[] il = this.SerializeMethodBodyIL(methodBody);

            // serialization only replaces fake tokens with real tokens, it doesn't remove/insert bytecodes:
            Debug.Assert(il.Length == ilLength);

            if (isSmallBody)
            {
                // If 'possiblyDuplicateMethodBodies' is not null, check if an identical
                // method body has already been serialized. If so, use the RVA
                // of the already serialized one. 
                if (possiblyDuplicateMethodBodies != null)
                {
                    if (!possiblyDuplicateMethodBodies.TryGetValue(il, out bodyRva))
                    {
                        possiblyDuplicateMethodBodies.Add(il, writer.BaseStream.Position);
                    }
                }
            }

            if (bodyRva == 0)
            {
                if (isSmallBody)
                {
                    bodyRva = writer.BaseStream.Position;
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
            }

            this.OnSerializedMethodBody(methodBody);

            return bodyRva;
        }

        private uint SerializeLocalVariablesSignature(ImmutableArray<ILocalDefinition> localVariables)
        {
            Debug.Assert(!this.tableIndicesAreComplete);

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

            uint blobIndex = this.GetBlobIndex(writer.BaseStream.ToArray());
            uint signatureIndex = this.GetOrAddStandAloneSignatureIndex(blobIndex);
            stream.Free();

            return 0x11000000 | signatureIndex;
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
            uint blobIndex = this.GetBlobIndex(sig.ToArray());
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
                            uint newToken = this.pseudoSymbolTokenToTokenMap[(int)currentToken];
                            WriteUint(methodBodyIL, newToken, curIndex);
                            curIndex += 4;
                        }
                        break;

                    case OperandType.InlineString:
                        {
                            uint currentToken = ReadUint(methodBodyIL, curIndex);
                            uint newToken = this.pseudoStringTokenToTokenMap[(int)currentToken];
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
                        writer.WriteCompressedInt(lowerBound);
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

                    /* Roslyn's uninstantiated type is the same object as the instantiated type for
                     * types closed over their type parameters, so to speak.
                  if (uninstantiatedTypeReference == typeReference) {
                    // TODO: error
                    return;
                  }
                     */
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

        ////
        //// Resource Format.
        ////

        ////
        //// Resource directory consists of two counts, following by a variable length
        //// array of directory entries.  The first count is the number of entries at
        //// beginning of the array that have actual names associated with each entry.
        //// The entries are in ascending order, case insensitive strings.  The second
        //// count is the number of entries that immediately follow the named entries.
        //// This second count identifies the number of entries that have 16-bit integer
        //// Ids as their name.  These entries are also sorted in ascending order.
        ////
        //// This structure allows fast lookup by either name or number, but for any
        //// given resource entry only one form of lookup is supported, not both.
        //// This is consistant with the syntax of the .RC file and the .RES file.
        ////

        //typedef struct _IMAGE_RESOURCE_DIRECTORY {
        //    DWORD   Characteristics;
        //    DWORD   TimeDateStamp;
        //    WORD    MajorVersion;
        //    WORD    MinorVersion;
        //    WORD    NumberOfNamedEntries;
        //    WORD    NumberOfIdEntries;
        ////  IMAGE_RESOURCE_DIRECTORY_ENTRY DirectoryEntries[];
        //} IMAGE_RESOURCE_DIRECTORY, *PIMAGE_RESOURCE_DIRECTORY;

        //#define IMAGE_RESOURCE_NAME_IS_STRING        0x80000000
        //#define IMAGE_RESOURCE_DATA_IS_DIRECTORY     0x80000000
        ////
        //// Each directory contains the 32-bit Name of the entry and an offset,
        //// relative to the beginning of the resource directory of the data associated
        //// with this directory entry.  If the name of the entry is an actual text
        //// string instead of an integer Id, then the high order bit of the name field
        //// is set to one and the low order 31-bits are an offset, relative to the
        //// beginning of the resource directory of the string, which is of type
        //// IMAGE_RESOURCE_DIRECTORY_STRING.  Otherwise the high bit is clear and the
        //// low-order 16-bits are the integer Id that identify this resource directory
        //// entry. If the directory entry is yet another resource directory (i.e. a
        //// subdirectory), then the high order bit of the offset field will be
        //// set to indicate this.  Otherwise the high bit is clear and the offset
        //// field points to a resource data entry.
        ////

        //typedef struct _IMAGE_RESOURCE_DIRECTORY_ENTRY {
        //    union {
        //        struct {
        //            DWORD NameOffset:31;
        //            DWORD NameIsString:1;
        //        } DUMMYSTRUCTNAME;
        //        DWORD   Name;
        //        WORD    Id;
        //    } DUMMYUNIONNAME;
        //    union {
        //        DWORD   OffsetToData;
        //        struct {
        //            DWORD   OffsetToDirectory:31;
        //            DWORD   DataIsDirectory:1;
        //        } DUMMYSTRUCTNAME2;
        //    } DUMMYUNIONNAME2;
        //} IMAGE_RESOURCE_DIRECTORY_ENTRY, *PIMAGE_RESOURCE_DIRECTORY_ENTRY;

        ////
        //// For resource directory entries that have actual string names, the Name
        //// field of the directory entry points to an object of the following type.
        //// All of these string objects are stored together after the last resource
        //// directory entry and before the first resource data object.  This minimizes
        //// the impact of these variable length objects on the alignment of the fixed
        //// size directory entry objects.
        ////

        //typedef struct _IMAGE_RESOURCE_DIRECTORY_STRING {
        //    WORD    Length;
        //    CHAR    NameString[ 1 ];
        //} IMAGE_RESOURCE_DIRECTORY_STRING, *PIMAGE_RESOURCE_DIRECTORY_STRING;


        //typedef struct _IMAGE_RESOURCE_DIR_STRING_U {
        //    WORD    Length;
        //    WCHAR   NameString[ 1 ];
        //} IMAGE_RESOURCE_DIR_STRING_U, *PIMAGE_RESOURCE_DIR_STRING_U;


        ////
        //// Each resource data entry describes a leaf node in the resource directory
        //// tree.  It contains an offset, relative to the beginning of the resource
        //// directory of the data for the resource, a size field that gives the number
        //// of bytes of data at that offset, a CodePage that should be used when
        //// decoding code point values within the resource data.  Typically for new
        //// applications the code page would be the unicode code page.
        ////

        //typedef struct _IMAGE_RESOURCE_DATA_ENTRY {
        //    DWORD   OffsetToData;
        //    DWORD   Size;
        //    DWORD   CodePage;
        //    DWORD   Reserved;
        //} IMAGE_RESOURCE_DATA_ENTRY, *PIMAGE_RESOURCE_DATA_ENTRY;

        private class Directory
        {
            internal string Name;
            internal int ID;
            internal ushort NumberOfNamedEntries;
            internal ushort NumberOfIdEntries;
            internal List<object> Entries;

            internal Directory(string name, int id)
            {
                this.Name = name;
                this.ID = id;
                this.Entries = new List<object>();
            }
        }

        private static int CompareResources(IWin32Resource left, IWin32Resource right)
        {
            int result = CompareResourceIdentifiers(left.TypeId, left.TypeName, right.TypeId, right.TypeName);

            return (result == 0) ? CompareResourceIdentifiers(left.Id, left.Name, right.Id, right.Name) : result;
        }

        //when comparing a string vs ordinal, the string should always be less than the ordinal. Per the spec,
        //entries identified by string must precede those identified by ordinal.
        private static int CompareResourceIdentifiers(int xOrdinal, string xString, int yOrdinal, string yString)
        {
            if (xString == null)
            {
                if (yString == null)
                {
                    return xOrdinal - yOrdinal;
                }
                else
                {
                    return 1;
                }
            }
            else if (yString == null)
            {
                return -1;
            }
            else
            {
                return String.Compare(xString, yString, StringComparison.OrdinalIgnoreCase);
            }
        }

        //sort the resources by ID least to greatest then by NAME.
        //Where strings and ordinals are compared, strings are less than ordinals.
        internal static IEnumerable<IWin32Resource> SortResources(IEnumerable<IWin32Resource> resources)
        {
            return resources.OrderBy(CompareResources);
        }

        //Win32 resources are supplied to the compiler in one of two forms, .RES (the output of the resource compiler),
        //or .OBJ (the output of running cvtres.exe on a .RES file). A .RES file is parsed and processed into
        //a set of objects implementing IWin32Resources. These are then ordered and the final image form is constructed
        //and written to the resource section. Resources in .OBJ form are already very close to their final output
        //form. Rather than reading them and parsing them into a set of objects similar to those produced by 
        //processing a .RES file, we process them like the native linker would, copy the relevant sections from 
        //the .OBJ into our output and apply some fixups.
        private void SerializeWin32Resources()
        {
            var resourceSection = this.module.Win32ResourceSection;
            if (resourceSection != null)
            {
                SerializeWin32Resources(resourceSection);
                return;
            }

            var theResources = this.module.Win32Resources;

            if (IteratorHelper.EnumerableIsEmpty(theResources))
            {
                return;
            }

            SerializeWin32Resources(theResources);
        }

        private void SerializeWin32Resources(IEnumerable<IWin32Resource> theResources)
        {
            theResources = SortResources(theResources);

            Directory typeDirectory = new Directory(string.Empty, 0);
            Directory nameDirectory = null;
            Directory languageDirectory = null;
            int lastTypeID = int.MinValue;
            string lastTypeName = null;
            int lastID = int.MinValue;
            string lastName = null;
            uint sizeOfDirectoryTree = 16;

            //EDMAURER note that this list is assumed to be sorted lowest to highest 
            //first by typeId, then by Id.
            foreach (IWin32Resource r in theResources)
            {
                bool typeDifferent = (r.TypeId < 0 && r.TypeName != lastTypeName) || r.TypeId > lastTypeID;
                if (typeDifferent)
                {
                    lastTypeID = r.TypeId;
                    lastTypeName = r.TypeName;
                    if (lastTypeID < 0)
                    {
                        Debug.Assert(typeDirectory.NumberOfIdEntries == 0, "Not all Win32 resources with types encoded as strings precede those encoded as ints");
                        typeDirectory.NumberOfNamedEntries++;
                    }
                    else
                    {
                        typeDirectory.NumberOfIdEntries++;
                    }

                    sizeOfDirectoryTree += 24;
                    typeDirectory.Entries.Add(nameDirectory = new Directory(lastTypeName, lastTypeID));
                }

                if (typeDifferent || (r.Id < 0 && r.Name != lastName) || r.Id > lastID)
                {
                    lastID = r.Id;
                    lastName = r.Name;
                    if (lastID < 0)
                    {
                        Debug.Assert(nameDirectory.NumberOfIdEntries == 0, "Not all Win32 resources with names encoded as strings precede those encoded as ints");
                        nameDirectory.NumberOfNamedEntries++;
                    }
                    else
                    {
                        nameDirectory.NumberOfIdEntries++;
                    }

                    sizeOfDirectoryTree += 24;
                    nameDirectory.Entries.Add(languageDirectory = new Directory(lastName, lastID));
                }

                languageDirectory.NumberOfIdEntries++;
                sizeOfDirectoryTree += 8;
                languageDirectory.Entries.Add(r);
            }

            MemoryStream stream = MemoryStream.GetInstance();
            BinaryWriter dataWriter = new BinaryWriter(stream, true);

            //'dataWriter' is where opaque resource data goes as well as strings that are used as type or name identifiers
            this.WriteDirectory(typeDirectory, this.win32ResourceWriter, 0, 0, sizeOfDirectoryTree, this.resourceSection.RelativeVirtualAddress, dataWriter);
            dataWriter.BaseStream.WriteTo(this.win32ResourceWriter.BaseStream);
            this.win32ResourceWriter.WriteByte(0);
            while ((this.win32ResourceWriter.BaseStream.Length % 4) != 0)
            {
                this.win32ResourceWriter.WriteByte(0);
            }
            stream.Free();
        }

        private void WriteDirectory(Directory directory, BinaryWriter writer, uint offset, uint level, uint sizeOfDirectoryTree, uint virtualAddressBase, BinaryWriter dataWriter)
        {
            writer.WriteUint(0); // Characteristics
            writer.WriteUint(0); // Timestamp
            writer.WriteUint(0); // Version
            writer.WriteUshort(directory.NumberOfNamedEntries);
            writer.WriteUshort(directory.NumberOfIdEntries);
            uint n = (uint)directory.Entries.Count;
            uint k = offset + 16 + n * 8;
            for (int i = 0; i < n; i++)
            {
                int id = int.MinValue;
                string name = null;
                uint nameOffset = dataWriter.BaseStream.Position + sizeOfDirectoryTree;
                uint directoryOffset = k;
                Directory subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    id = subDir.ID;
                    name = subDir.Name;
                    if (level == 0)
                    {
                        k += SizeOfDirectory(subDir);
                    }
                    else
                    {
                        k += 16 + 8 * (uint)subDir.Entries.Count;
                    }
                }
                else
                {
                    //EDMAURER write out an IMAGE_RESOURCE_DATA_ENTRY followed
                    //immediately by the data that it refers to. This results
                    //in a layout different than that produced by pulling the resources
                    //from an OBJ. In that case all of the data bits of a resource are
                    //contiguous in .rsrc$02. After processing these will end up at
                    //the end of .rsrc following all of the directory
                    //info and IMAGE_RESOURCE_DATA_ENTRYs
                    IWin32Resource r = (IWin32Resource)directory.Entries[i];
                    id = level == 0 ? r.TypeId : level == 1 ? r.Id : (int)r.LanguageId;
                    name = level == 0 ? r.TypeName : level == 1 ? r.Name : null;
                    dataWriter.WriteUint(virtualAddressBase + sizeOfDirectoryTree + 16 + dataWriter.BaseStream.Position);
                    byte[] data = new List<byte>(r.Data).ToArray();
                    dataWriter.WriteUint((uint)data.Length);
                    dataWriter.WriteUint(r.CodePage);
                    dataWriter.WriteUint(0);
                    dataWriter.WriteBytes(data);
                    while ((dataWriter.BaseStream.Length % 4) != 0)
                    {
                        dataWriter.WriteByte(0);
                    }
                }

                if (id >= 0)
                {
                    writer.WriteInt(id);
                }
                else
                {
                    if (name == null)
                    {
                        name = string.Empty;
                    }

                    writer.WriteUint(nameOffset | 0x80000000);
                    dataWriter.WriteUshort((ushort)name.Length);
                    dataWriter.WriteChars(name.ToCharArray());  // REVIEW: what happens if the name contains chars that do not fit into a single utf8 code point?
                }

                if (subDir != null)
                {
                    writer.WriteUint(directoryOffset | 0x80000000);
                }
                else
                {
                    writer.WriteUint(nameOffset);
                }
            }

            k = offset + 16 + n * 8;
            for (int i = 0; i < n; i++)
            {
                Directory subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    this.WriteDirectory(subDir, writer, k, level + 1, sizeOfDirectoryTree, virtualAddressBase, dataWriter);
                    if (level == 0)
                    {
                        k += SizeOfDirectory(subDir);
                    }
                    else
                    {
                        k += 16 + 8 * (uint)subDir.Entries.Count;
                    }
                }
            }
        }

        private static uint SizeOfDirectory(Directory/*!*/ directory)
        {
            uint n = (uint)directory.Entries.Count;
            uint size = 16 + 8 * n;
            for (int i = 0; i < n; i++)
            {
                Directory subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    size += 16 + 8 * (uint)subDir.Entries.Count;
                }
            }

            return size;
        }

        private void SerializeWin32Resources(ResourceSection resourceSections)
        {
            this.win32ResourceWriter.WriteBytes(resourceSections.SectionBytes);

            var savedPosition = this.win32ResourceWriter.BaseStream.Position;

            var readStream = new System.IO.MemoryStream(resourceSections.SectionBytes);
            var reader = new System.IO.BinaryReader(readStream);

            foreach (int addressToFixup in resourceSections.Relocations)
            {
                this.win32ResourceWriter.BaseStream.Position = (uint)addressToFixup;
                reader.BaseStream.Position = addressToFixup;
                this.win32ResourceWriter.WriteUint(reader.ReadUInt32() + this.resourceSection.RelativeVirtualAddress);
            }

            this.win32ResourceWriter.BaseStream.Position = savedPosition;
        }

        //#define IMAGE_FILE_RELOCS_STRIPPED           0x0001  // Relocation info stripped from file.
        //#define IMAGE_FILE_EXECUTABLE_IMAGE          0x0002  // File is executable  (i.e. no unresolved externel references).
        //#define IMAGE_FILE_LINE_NUMS_STRIPPED        0x0004  // Line nunbers stripped from file.
        //#define IMAGE_FILE_LOCAL_SYMS_STRIPPED       0x0008  // Local symbols stripped from file.
        //#define IMAGE_FILE_AGGRESIVE_WS_TRIM         0x0010  // Agressively trim working set
        //#define IMAGE_FILE_LARGE_ADDRESS_AWARE       0x0020  // App can handle >2gb addresses
        //#define IMAGE_FILE_BYTES_REVERSED_LO         0x0080  // Bytes of machine word are reversed.
        //#define IMAGE_FILE_32BIT_MACHINE             0x0100  // 32 bit word machine.
        //#define IMAGE_FILE_DEBUG_STRIPPED            0x0200  // Debugging info stripped from file in .DBG file
        //#define IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP   0x0400  // If Image is on removable media, copy and run from the swap file.
        //#define IMAGE_FILE_NET_RUN_FROM_SWAP         0x0800  // If Image is on Net, copy and run from the swap file.
        //#define IMAGE_FILE_SYSTEM                    0x1000  // System File.
        //#define IMAGE_FILE_DLL                       0x2000  // File is a DLL.
        //#define IMAGE_FILE_UP_SYSTEM_ONLY            0x4000  // File should only be run on a UP machine
        //#define IMAGE_FILE_BYTES_REVERSED_HI         0x8000  // Bytes of machine word are reversed.

        private void WriteHeaders(Stream peStream, out long timestampOffset)
        {
            IModule module = this.module;
            NtHeader ntHeader = this.ntHeader;
            BinaryWriter writer = new BinaryWriter(this.headerStream);

            // MS-DOS stub (128 bytes)
            writer.WriteBytes(dosHeader); // TODO: provide an option to suppress the second half of the DOS header?

            // PE Signature (4 bytes)
            writer.WriteUint(0x00004550); /* "PE\0\0" */

            // COFF Header 20 bytes
            writer.WriteUshort((ushort)module.Machine);
            writer.WriteUshort((ushort)ntHeader.NumberOfSections);
            timestampOffset = (uint)(writer.BaseStream.Position + peStream.Position);
            writer.WriteUint(ntHeader.TimeDateStamp);
            writer.WriteUint(ntHeader.PointerToSymbolTable);
            writer.WriteUint(0); // NumberOfSymbols
            writer.WriteUshort((ushort)(!module.Requires64bits ? 224 : 240)); // SizeOfOptionalHeader
            // ushort characteristics = 0x0002|0x0004|0x0008; // executable | no COFF line nums | no COFF symbols (as required by the standard)
            ushort characteristics = 0x0002; // executable (as required by the Linker team).
            if (module.Kind == ModuleKind.DynamicallyLinkedLibrary || module.Kind == ModuleKind.WindowsRuntimeMetadata)
            {
                characteristics |= 0x2000;
            }

            if (module.Requires32bits)
            {
                characteristics |= 0x0100; // 32 bit machine (The standard says to always set this, the linker team says otherwise)
                //The loader team says that this is not used for anything in the OS. 
            }
            else
            {
                characteristics |= 0x0020; // large address aware (the standard says never to set this, the linker team says otherwise).
                //The loader team says that this is not overridden for managed binaries and will be respected if set.
            }

            writer.WriteUshort(characteristics);

            // PE Header (224 bytes if 32 bits, 240 bytes if 64 bit)
            if (!module.Requires64bits)
            {
                writer.WriteUshort(0x10B); // Magic = PE32  // 2
            }
            else
            {
                writer.WriteUshort(0x20B); // Magic = PE32+ // 2
            }

            writer.WriteByte(module.LinkerMajorVersion); // 3
            writer.WriteByte(module.LinkerMinorVersion); // 4
            writer.WriteUint(ntHeader.SizeOfCode); // 8
            writer.WriteUint(ntHeader.SizeOfInitializedData); // 12
            writer.WriteUint(ntHeader.SizeOfUninitializedData); // 16
            writer.WriteUint(ntHeader.AddressOfEntryPoint); // 20
            writer.WriteUint(ntHeader.BaseOfCode); // 24
            if (!module.Requires64bits)
            {
                writer.WriteUint(ntHeader.BaseOfData); // 28
                writer.WriteUint((uint)module.BaseAddress); // 32
            }
            else
            {
                writer.WriteUlong(module.BaseAddress); // 32
            }

            writer.WriteUint(0x2000); // SectionAlignment 36
            writer.WriteUint(module.FileAlignment); // 40
            writer.WriteUshort(4); // MajorOperatingSystemVersion 42
            writer.WriteUshort(0); // MinorOperatingSystemVersion 44
            writer.WriteUshort(0); // MajorImageVersion 46
            writer.WriteUshort(0); // MinorImageVersion 48
            writer.WriteUshort(module.MajorSubsystemVersion); // MajorSubsystemVersion 50
            writer.WriteUshort(module.MinorSubsystemVersion); // MinorSubsystemVersion 52
            writer.WriteUint(0); // Win32VersionValue 56
            writer.WriteUint(ntHeader.SizeOfImage); // 60
            writer.WriteUint(ntHeader.SizeOfHeaders); // 64
            writer.WriteUint(0); // CheckSum 68
            switch (module.Kind)
            {
                case ModuleKind.ConsoleApplication:
                case ModuleKind.DynamicallyLinkedLibrary:
                case ModuleKind.WindowsRuntimeMetadata:
                    writer.WriteUshort(3); // 70
                    break;
                case ModuleKind.WindowsApplication:
                    writer.WriteUshort(2); // 70
                    break;
                default:
                    writer.WriteUshort(0); //
                    break;
            }

            writer.WriteUshort(module.DllCharacteristics);

            if (!module.Requires64bits)
            {
                writer.WriteUint((uint)module.SizeOfStackReserve); // 76
                writer.WriteUint((uint)module.SizeOfStackCommit); // 80
                writer.WriteUint((uint)module.SizeOfHeapReserve); // 84
                writer.WriteUint((uint)module.SizeOfHeapCommit); // 88
            }
            else
            {
                writer.WriteUlong(module.SizeOfStackReserve); // 80
                writer.WriteUlong(module.SizeOfStackCommit); // 88
                writer.WriteUlong(module.SizeOfHeapReserve); // 96
                writer.WriteUlong(module.SizeOfHeapCommit); // 104
            }

            writer.WriteUint(0); // LoaderFlags 92|108
            writer.WriteUint(16); // numberOfDataDirectories 96|112

            writer.WriteUint(ntHeader.ExportTable.RelativeVirtualAddress); // 100|116
            writer.WriteUint(ntHeader.ExportTable.Size); // 104|120
            writer.WriteUint(ntHeader.ImportTable.RelativeVirtualAddress); // 108|124
            writer.WriteUint(ntHeader.ImportTable.Size); // 112|128
            writer.WriteUint(ntHeader.ResourceTable.RelativeVirtualAddress); // 116|132
            writer.WriteUint(ntHeader.ResourceTable.Size); // 120|136
            writer.WriteUint(ntHeader.ExceptionTable.RelativeVirtualAddress); // 124|140
            writer.WriteUint(ntHeader.ExceptionTable.Size); // 128|144
            writer.WriteUint(ntHeader.CertificateTable.RelativeVirtualAddress); // 132|148
            writer.WriteUint(ntHeader.CertificateTable.Size); // 136|152
            writer.WriteUint(ntHeader.BaseRelocationTable.RelativeVirtualAddress); // 140|156
            writer.WriteUint(ntHeader.BaseRelocationTable.Size); // 144|160
            writer.WriteUint(ntHeader.DebugTable.RelativeVirtualAddress); // 148|164
            writer.WriteUint(ntHeader.DebugTable.Size); // 152|168
            writer.WriteUint(ntHeader.CopyrightTable.RelativeVirtualAddress); // 156|172
            writer.WriteUint(ntHeader.CopyrightTable.Size); // 160|176
            writer.WriteUint(ntHeader.GlobalPointerTable.RelativeVirtualAddress); // 164|180
            writer.WriteUint(ntHeader.GlobalPointerTable.Size); // 168|184
            writer.WriteUint(ntHeader.ThreadLocalStorageTable.RelativeVirtualAddress); // 172|188
            writer.WriteUint(ntHeader.ThreadLocalStorageTable.Size); // 176|192
            writer.WriteUint(ntHeader.LoadConfigTable.RelativeVirtualAddress); // 180|196
            writer.WriteUint(ntHeader.LoadConfigTable.Size); // 184|200
            writer.WriteUint(ntHeader.BoundImportTable.RelativeVirtualAddress); // 188|204
            writer.WriteUint(ntHeader.BoundImportTable.Size); // 192|208
            writer.WriteUint(ntHeader.ImportAddressTable.RelativeVirtualAddress); // 196|212
            writer.WriteUint(ntHeader.ImportAddressTable.Size); // 200|216
            writer.WriteUint(ntHeader.DelayImportTable.RelativeVirtualAddress); // 204|220
            writer.WriteUint(ntHeader.DelayImportTable.Size); // 208|224
            writer.WriteUint(ntHeader.CliHeaderTable.RelativeVirtualAddress); // 212|228
            writer.WriteUint(ntHeader.CliHeaderTable.Size); // 216|232
            writer.WriteUlong(0); // 224|240

            // Section Headers
            WriteSectionHeader(this.textSection, writer);
            WriteSectionHeader(this.rdataSection, writer);
            WriteSectionHeader(this.sdataSection, writer);
            WriteSectionHeader(this.coverSection, writer);
            WriteSectionHeader(this.resourceSection, writer);
            WriteSectionHeader(this.relocSection, writer);
            WriteSectionHeader(this.tlsSection, writer);

            writer.BaseStream.WriteTo(peStream);
            this.headerStream = this.emptyStream;
        }

        private static void WriteSectionHeader(SectionHeader sectionHeader, BinaryWriter writer)
        {
            if (sectionHeader.VirtualSize == 0)
            {
                return;
            }

            for (int j = 0, m = sectionHeader.Name.Length; j < 8; j++)
            {
                if (j < m)
                {
                    writer.WriteByte((byte)sectionHeader.Name[j]);
                }
                else
                {
                    writer.WriteByte(0);
                }
            }

            writer.WriteUint(sectionHeader.VirtualSize);
            writer.WriteUint(sectionHeader.RelativeVirtualAddress);
            writer.WriteUint(sectionHeader.SizeOfRawData);
            writer.WriteUint(sectionHeader.PointerToRawData);
            writer.WriteUint(sectionHeader.PointerToRelocations);
            writer.WriteUint(sectionHeader.PointerToLinenumbers);
            writer.WriteUshort(sectionHeader.NumberOfRelocations);
            writer.WriteUshort(sectionHeader.NumberOfLinenumbers);
            writer.WriteUint(sectionHeader.Characteristics);
        }

        private void WriteTextSection(Stream peStream, MemoryStream metadataStream, MemoryStream ilStream, out long startOfMetadata, out long positionOfTimestamp)
        {
            peStream.Position = this.textSection.PointerToRawData;
            if (this.emitRuntimeStartupStub) this.WriteImportAddressTable(peStream);
            this.WriteClrHeader(peStream);
            this.WriteIL(peStream, ilStream);
            startOfMetadata = peStream.Position;
            this.WriteMetadata(peStream, metadataStream);
            this.WriteManagedResources(peStream);
            this.WriteSpaceForHash(peStream);
            this.WriteDebugTable(peStream, out positionOfTimestamp);
            // this.WriteUnmanagedExportStubs();
            if (this.emitRuntimeStartupStub) this.WriteImportTable(peStream);
            if (this.emitRuntimeStartupStub) this.WriteNameTable(peStream);
            if (this.emitRuntimeStartupStub) this.WriteRuntimeStartupStub(peStream);
            this.WriteTextData(peStream);
        }

        private void WriteImportAddressTable(Stream peStream)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(16));
            bool use32bitAddresses = !this.module.Requires64bits;
            uint importTableRVA = this.ntHeader.ImportTable.RelativeVirtualAddress;
            uint ilRVA = importTableRVA + 40;
            uint hintRva = ilRVA + (use32bitAddresses ? 12u : 16u);

            // Import Address Table
            if (use32bitAddresses)
            {
                writer.WriteUint(hintRva); // 4
                writer.WriteUint(0); // 8
            }
            else
            {
                writer.WriteUlong(hintRva); // 8
                writer.WriteUlong(0); // 16
            }

            writer.BaseStream.WriteTo(peStream);
        }

        private void WriteImportTable(Stream peStream)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(70));
            bool use32bitAddresses = !this.module.Requires64bits;
            uint importTableRVA = this.ntHeader.ImportTable.RelativeVirtualAddress;
            uint ilRVA = importTableRVA + 40;
            uint hintRva = ilRVA + (use32bitAddresses ? 12u : 16u);
            uint nameRva = hintRva + 12 + 2;

            // Import table
            writer.WriteUint(ilRVA); // 4
            writer.WriteUint(0); // 8
            writer.WriteUint(0); // 12
            writer.WriteUint(nameRva); // 16
            writer.WriteUint(this.ntHeader.ImportAddressTable.RelativeVirtualAddress); // 20
            writer.BaseStream.Position += 20; // 40

            // Import Lookup table
            if (use32bitAddresses)
            {
                writer.WriteUint(hintRva); // 44
                writer.WriteUint(0); // 48
                writer.WriteUint(0); // 52
            }
            else
            {
                writer.WriteUlong(hintRva); // 48
                writer.WriteUlong(0); // 56
            }

            // Hint table
            writer.WriteUshort(0); // Hint 54|58
            string entryPointName =
                (this.module.Kind == ModuleKind.DynamicallyLinkedLibrary || this.module.Kind == ModuleKind.WindowsRuntimeMetadata)
                ? "_CorDllMain" : "_CorExeMain";

            foreach (char ch in entryPointName)
            {
                writer.WriteByte((byte)ch); // 65|69
            }

            writer.WriteByte(0); // 66|70

            writer.BaseStream.WriteTo(peStream);
        }

        private void WriteNameTable(Stream peStream)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(14));
            foreach (char ch in "mscoree.dll")
            {
                writer.WriteByte((byte)ch); // 11
            }

            writer.WriteByte(0); // 12
            writer.WriteUshort(0); // 14
            writer.BaseStream.WriteTo(peStream);
        }

        private void WriteClrHeader(Stream peStream)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(72));
            ClrHeader clrHeader = this.clrHeader;
            writer.WriteUint(72); // Number of bytes in this header  4
            writer.WriteUshort(clrHeader.MajorRuntimeVersion); // 6 
            writer.WriteUshort(clrHeader.MinorRuntimeVersion); // 8
            writer.WriteUint(clrHeader.MetaData.RelativeVirtualAddress); // 12
            writer.WriteUint(clrHeader.MetaData.Size); // 16
            writer.WriteUint(clrHeader.Flags); // 20
            writer.WriteUint(clrHeader.EntryPointToken); // 24
            writer.WriteUint(clrHeader.Resources.Size == 0 ? 0u : clrHeader.Resources.RelativeVirtualAddress); // 28
            writer.WriteUint(clrHeader.Resources.Size); // 32
            writer.WriteUint(clrHeader.StrongNameSignature.Size == 0 ? 0u : clrHeader.StrongNameSignature.RelativeVirtualAddress); // 36
            writer.WriteUint(clrHeader.StrongNameSignature.Size); // 40
            writer.WriteUint(clrHeader.CodeManagerTable.RelativeVirtualAddress); // 44
            writer.WriteUint(clrHeader.CodeManagerTable.Size); // 48
            writer.WriteUint(clrHeader.VTableFixups.RelativeVirtualAddress); // 52
            writer.WriteUint(clrHeader.VTableFixups.Size); // 56
            writer.WriteUint(clrHeader.ExportAddressTableJumps.RelativeVirtualAddress); // 60
            writer.WriteUint(clrHeader.ExportAddressTableJumps.Size); // 64
            writer.WriteUlong(0); // 72
            writer.BaseStream.WriteTo(peStream);
        }

        private void WriteIL(Stream peStream, MemoryStream ilStream)
        {
            ilStream.WriteTo(peStream);
            while (peStream.Position % 4 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private void WriteTextData(Stream peStream)
        {
            this.textDataWriter.BaseStream.WriteTo(peStream);
            while (peStream.Position % 4 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private void WriteSpaceForHash(Stream peStream)
        {
            uint size = this.clrHeader.StrongNameSignature.Size;
            while (size > 0)
            {
                peStream.WriteByte(0);
                size--;
            }
        }

        private void WriteMetadata(Stream peStream, MemoryStream metadataStream)
        {
            metadataStream.WriteTo(peStream);
            while (peStream.Position % 4 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private void WriteManagedResources(Stream peStream)
        {
            this.resourceWriter.BaseStream.WriteTo(peStream);
            while (peStream.Position % 4 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private void WriteDebugTable(Stream peStream, out long timestampOffset)
        {
            timestampOffset = 0;
            PeDebugDirectory debugDirectory = this.debugDirectory;
            if (debugDirectory == null)
            {
                return;
            }

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.WriteUint(debugDirectory.Characteristics);
            timestampOffset = writer.BaseStream.Position + peStream.Position;
            writer.WriteUint(debugDirectory.TimeDateStamp);
            writer.WriteUshort(debugDirectory.MajorVersion);
            writer.WriteUshort(debugDirectory.MinorVersion);
            writer.WriteUint(debugDirectory.Type);
            writer.WriteUint(debugDirectory.SizeOfData);
            debugDirectory.AddressOfRawData = debugDirectory.PointerToRawData + this.textSection.RelativeVirtualAddress;
            debugDirectory.PointerToRawData += this.textSection.PointerToRawData;
            writer.WriteUint(debugDirectory.AddressOfRawData);
            writer.WriteUint(debugDirectory.PointerToRawData);
            writer.WriteBytes(debugDirectory.Data);
            writer.BaseStream.WriteTo(peStream);
            stream.Free();
        }

        // private void WriteUnmanagedExportStubs() { }

        private void WriteRuntimeStartupStub(Stream peStream)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(16));
            // entry point code, consisting of a jump indirect to _CorXXXMain
            if (!this.module.Requires64bits)
            {
                //emit 0's (nops) to pad the entry point code so that the target address is aligned on a 4 byte boundary.
                for (uint i = 0, n = (uint)(Aligned((uint)peStream.Position, 4) - peStream.Position); i < n; i++) writer.WriteByte(0);
                writer.WriteUshort(0);
                writer.WriteByte(0xff);
                writer.WriteByte(0x25); //4
                writer.WriteUint(this.ntHeader.ImportAddressTable.RelativeVirtualAddress + (uint)this.module.BaseAddress); //8
            }
            else
            {
                //emit 0's (nops) to pad the entry point code so that the target address is aligned on a 8 byte boundary.
                for (uint i = 0, n = (uint)(Aligned((uint)peStream.Position, 8) - peStream.Position); i < n; i++) writer.WriteByte(0);
                writer.WriteUint(0);
                writer.WriteUshort(0);
                writer.WriteByte(0xff);
                writer.WriteByte(0x25); //8
                writer.WriteUlong(this.ntHeader.ImportAddressTable.RelativeVirtualAddress + this.module.BaseAddress); //16
            }
            writer.BaseStream.WriteTo(peStream);
        }

        private void WriteCoverSection(Stream peStream)
        {
            peStream.Position = this.coverSection.PointerToRawData;
            this.coverageDataWriter.BaseStream.WriteTo(peStream);
        }

        private void WriteRdataSection(Stream peStream)
        {
            peStream.Position = this.rdataSection.PointerToRawData;
            this.rdataWriter.BaseStream.WriteTo(peStream);
        }

        private void WriteSdataSection(Stream peStream)
        {
            peStream.Position = this.sdataSection.PointerToRawData;
            this.sdataWriter.BaseStream.WriteTo(peStream);
        }

        private void WriteRelocSection(Stream peStream)
        {
            if (!this.emitRuntimeStartupStub)
            {
                //No need to write out a reloc section, but there is still a need to pad out the peStream so that it is an even multiple of module.FileAlignment
                if (this.relocSection.PointerToRawData != peStream.Position)
                { //for example, the resource section did not end bang on the alignment boundary
                    peStream.Position = this.relocSection.PointerToRawData - 1;
                    peStream.WriteByte(0);
                }
                return;
            }

            peStream.Position = this.relocSection.PointerToRawData;
            BinaryWriter writer = new BinaryWriter(new MemoryStream(this.module.FileAlignment));
            writer.WriteUint(((this.ntHeader.AddressOfEntryPoint + 2) / 0x1000) * 0x1000);
            writer.WriteUint(this.module.Requires64bits && !this.module.RequiresAmdInstructionSet ? 14u : 12u);
            uint offsetWithinPage = (this.ntHeader.AddressOfEntryPoint + 2) % 0x1000;
            uint relocType = this.module.Requires64bits ? 10u : 3u;
            ushort s = (ushort)((relocType << 12) | offsetWithinPage);
            writer.WriteUshort(s);
            if (this.module.Requires64bits && !this.module.RequiresAmdInstructionSet)
            {
                writer.WriteUint(relocType << 12);
            }

            writer.WriteUshort(0); // next chunk's RVA
            writer.BaseStream.Position = this.module.FileAlignment;
            writer.BaseStream.WriteTo(peStream);
        }

        private void WriteResourceSection(Stream peStream)
        {
            if (this.win32ResourceWriter.BaseStream.Length == 0)
            {
                return;
            }

            peStream.Position = this.resourceSection.PointerToRawData;
            this.win32ResourceWriter.BaseStream.WriteTo(peStream);
            peStream.WriteByte(0);
            while (peStream.Position % 8 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private void WriteTlsSection(Stream peStream)
        {
            peStream.Position = this.tlsSection.PointerToRawData;
            this.tlsDataWriter.BaseStream.WriteTo(peStream);
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
            private readonly PeWriter writer;
            private readonly List<T> rows;
            private readonly uint firstRowId;

            public HeapOrReferenceIndexBase(PeWriter writer, uint lastRowId)
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

            public HeapOrReferenceIndex(PeWriter writer, uint lastRowId = 0) :
                this(writer, new Dictionary<T, uint>(), lastRowId)
            {
            }

            public HeapOrReferenceIndex(PeWriter writer, IEqualityComparer<T> comparer, uint lastRowId = 0) :
                this(writer, new Dictionary<T, uint>(comparer), lastRowId)
            {
            }

            private HeapOrReferenceIndex(PeWriter writer, Dictionary<T, uint> index, uint lastRowId) :
                base(writer, lastRowId)
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

            public InstanceAndStructuralReferenceIndex(PeWriter writer, IEqualityComparer<T> structuralComparer, uint lastRowId = 0) :
                base(writer, lastRowId)
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

    internal enum HeapSizeFlag : byte
    {
        StringHeapLarge = 0x01, // 4 byte uint indexes used for string heap offsets
        GuidHeapLarge = 0x02,   // 4 byte uint indexes used for GUID heap offsets
        BlobHeapLarge = 0x04,   // 4 byte uint indexes used for Blob heap offsets
        EnCDeltas = 0x20,       // Indicates only EnC Deltas are present
        DeletedMarks = 0x80,    // Indicates metadata might contain items marked deleted
    }

    internal static class TokenTypeIds
    {
        internal const uint Module = 0x00000000;
        internal const uint TypeRef = 0x01000000;
        internal const uint TypeDef = 0x02000000;
        internal const uint FieldDef = 0x04000000;
        internal const uint MethodDef = 0x06000000;
        internal const uint ParamDef = 0x08000000;
        internal const uint InterfaceImpl = 0x09000000;
        internal const uint MemberRef = 0x0a000000;
        internal const uint Constant = 0x0b000000;
        internal const uint CustomAttribute = 0x0c000000;
        internal const uint Permission = 0x0e000000;
        internal const uint Signature = 0x11000000;
        internal const uint EventMap = 0x12000000;
        internal const uint Event = 0x14000000;
        internal const uint PropertyMap = 0x15000000;
        internal const uint Property = 0x17000000;
        internal const uint MethodSemantics = 0x18000000;
        internal const uint MethodImpl = 0x19000000;
        internal const uint ModuleRef = 0x1a000000;
        internal const uint TypeSpec = 0x1b000000;
        internal const uint Assembly = 0x20000000;
        internal const uint AssemblyRef = 0x23000000;
        internal const uint File = 0x26000000;
        internal const uint ExportedType = 0x27000000;
        internal const uint ManifestResource = 0x28000000;
        internal const uint NestedClass = 0x29000000;
        internal const uint GenericParam = 0x2a000000;
        internal const uint MethodSpec = 0x2b000000;
        internal const uint GenericParamConstraint = 0x2c000000;
        internal const uint UserString = 0x70000000;
        internal const uint String = 0x71000000;
    }

    internal enum TypeFlags : uint
    {
        PrivateAccess = 0x00000000,
        PublicAccess = 0x00000001,
        NestedPublicAccess = 0x00000002,
        NestedPrivateAccess = 0x00000003,
        NestedFamilyAccess = 0x00000004,
        NestedAssemblyAccess = 0x00000005,
        NestedFamilyAndAssemblyAccess = 0x00000006,
        NestedFamilyOrAssemblyAccess = 0x00000007,
        AccessMask = 0x0000007,
        NestedMask = 0x00000006,

        AutoLayout = 0x00000000,
        SequentialLayout = 0x00000008,
        ExplicitLayout = 0x00000010,
        LayoutMask = 0x00000018,

        ClassSemantics = 0x00000000,
        InterfaceSemantics = 0x00000020,
        AbstractSemantics = 0x00000080,
        SealedSemantics = 0x00000100,
        SpecialNameSemantics = 0x00000400,

        ImportImplementation = 0x00001000,
        SerializableImplementation = 0x00002000,
        WindowsRuntimeImplementation = 0x00004000,
        BeforeFieldInitImplementation = 0x00100000,
        ForwarderImplementation = 0x00200000,

        AnsiString = 0x00000000,
        UnicodeString = 0x00010000,
        AutoCharString = 0x00020000,
        StringMask = 0x00030000,

        RTSpecialNameReserved = 0x00000800,
        HasSecurityReserved = 0x00040000,
    }
}
