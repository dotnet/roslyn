// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal abstract partial class MetadataWriter
    {
        private static readonly Encoding s_utf8Encoding = Encoding.UTF8;

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

        private readonly int _numTypeDefsEstimate;
        private readonly bool _deterministic;

        // If true, it is allowed to have methods not have bodies (for emitting metadata-only assembly)
        internal readonly bool allowMissingMethodBodies;

        // A map of method body before token translation to RVA. Used for deduplication of small bodies.
        private readonly Dictionary<ImmutableArray<byte>, int> _smallMethodBodies;

        protected MetadataWriter(
            MetadataHeapsBuilder heaps,
            MetadataHeapsBuilder debugHeapsOpt,
            EmitContext context,
            CommonMessageProvider messageProvider,
            bool allowMissingMethodBodies,
            bool deterministic,
            CancellationToken cancellationToken)
        {
            this.module = context.Module;
            _deterministic = deterministic;
            this.allowMissingMethodBodies = allowMissingMethodBodies;

            // EDMAURER provide some reasonable size estimates for these that will avoid
            // much of the reallocation that would occur when growing these from empty.
            _signatureIndex = new Dictionary<ISignature, KeyValuePair<BlobIdx, ImmutableArray<byte>>>(module.HintNumberOfMethodDefinitions); //ignores field signatures

            _numTypeDefsEstimate = module.HintNumberOfMethodDefinitions / 6;
            _exportedTypeIndex = new Dictionary<ITypeReference, int>(_numTypeDefsEstimate);
            _exportedTypeList = new List<ITypeReference>(_numTypeDefsEstimate);

            this.Context = context;
            this.messageProvider = messageProvider;
            _cancellationToken = cancellationToken;

            this.heaps = heaps;
            _debugHeapsOpt = debugHeapsOpt;
            _smallMethodBodies = new Dictionary<ImmutableArray<byte>, int>(ByteSequenceComparer.Instance);
        }

        private int NumberOfTypeDefsEstimate { get { return _numTypeDefsEstimate; } }

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

        /// <summary>
        /// NetModules and EnC deltas don't have AssemblyDef record.
        /// We don't emit it for EnC deltas since assembly identity has to be preserved across generations (CLR/debugger get confused otherwise).
        /// </summary>
        private bool EmitAssemblyDefinition => module.AsAssembly != null && !IsMinimalDelta;

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
        /// Returns true and the 1-based index of the type definition
        /// if the type definition is recognized. Otherwise returns false.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract bool TryGetTypeDefIndex(ITypeDefinition def, out int index);

        /// <summary>
        /// The 1-based index of the type definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetTypeDefIndex(ITypeDefinition def);

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
        protected abstract int GetEventDefIndex(IEventDefinition def);

        /// <summary>
        /// The event definitions to be emitted, in row order. These
        /// are just the event definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IEventDefinition> GetEventDefs();

        /// <summary>
        /// The 1-based index of the field definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetFieldDefIndex(IFieldDefinition def);

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
        protected abstract bool TryGetMethodDefIndex(IMethodDefinition def, out int index);

        /// <summary>
        /// The 1-based index of the method definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetMethodDefIndex(IMethodDefinition def);

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
        protected abstract int GetPropertyDefIndex(IPropertyDefinition def);

        /// <summary>
        /// The property definitions to be emitted, in row order. These
        /// are just the property definitions from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<IPropertyDefinition> GetPropertyDefs();

        /// <summary>
        /// The 1-based index of the parameter definition.
        /// The index is into the full metadata.
        /// </summary>
        protected abstract int GetParameterDefIndex(IParameterDefinition def);

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
        protected abstract int GetFieldDefIndex(INamedTypeDefinition typeDef);

        /// <summary>
        /// The 1-based index of the first method of the type.
        /// </summary>
        protected abstract int GetMethodDefIndex(INamedTypeDefinition typeDef);

        /// <summary>
        /// The 1-based index of the first parameter of the method.
        /// </summary>
        protected abstract int GetParameterDefIndex(IMethodDefinition methodDef);

        /// <summary>
        /// Return the 1-based index of the assembly reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract int GetOrAddAssemblyRefIndex(IAssemblyReference reference);

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
        protected abstract int GetOrAddModuleRefIndex(string reference);

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
        protected abstract int GetOrAddMemberRefIndex(ITypeMemberReference reference);

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
        protected abstract int GetOrAddMethodSpecIndex(IGenericMethodInstanceReference reference);

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
        protected abstract bool TryGetTypeRefIndex(ITypeReference reference, out int index);

        /// <summary>
        /// Return the 1-based index of the type reference, adding
        /// the reference to the index for this generation if missing.
        /// The index is into the full metadata. However, deltas
        /// are not required to return rows from previous generations.
        /// </summary>
        protected abstract int GetOrAddTypeRefIndex(ITypeReference reference);

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
        protected abstract int GetOrAddTypeSpecIndex(ITypeReference reference);

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
        protected abstract int GetOrAddStandAloneSignatureIndex(BlobIdx blobIndex);

        /// <summary>
        /// The signature indices to be emitted, in row order. These
        /// are just the signature indices from the current generation.
        /// </summary>
        protected abstract IReadOnlyList<BlobIdx> GetStandAloneSignatures();

        protected abstract IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes(IModule module);

        protected abstract void CreateIndicesForNonTypeMembers(ITypeDefinition typeDef);

        /// <summary>
        /// Return a visitor for traversing all references to be emitted.
        /// </summary>
        protected abstract ReferenceIndexer CreateReferenceVisitor();

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
        private readonly CancellationToken _cancellationToken;
        protected readonly IModule module;
        public readonly EmitContext Context;
        protected readonly CommonMessageProvider messageProvider;

        // progress:
        private bool _tableIndicesAreComplete;

        private int[] _pseudoSymbolTokenToTokenMap;
        private IReference[] _pseudoSymbolTokenToReferenceMap;
        private int[] _pseudoStringTokenToTokenMap;
        private List<string> _pseudoStringTokenToStringMap;
        private ReferenceIndexer _referenceVisitor;

        protected readonly MetadataHeapsBuilder heaps;

        // A heap builder distinct from heaps if we are emitting debug information into a separate Portable PDB stream.
        // Shared heap builder (reference equals heaps) if we are embedding Portable PDB into the metadata stream.
        // Null otherwise.
        private readonly MetadataHeapsBuilder _debugHeapsOpt;

        private bool EmitStandaloneDebugMetadata => _debugHeapsOpt != null && heaps != _debugHeapsOpt;

        private readonly Dictionary<ICustomAttribute, BlobIdx> _customAttributeSignatureIndex = new Dictionary<ICustomAttribute, BlobIdx>();
        private readonly Dictionary<ITypeReference, BlobIdx> _typeSpecSignatureIndex = new Dictionary<ITypeReference, BlobIdx>();
        private readonly Dictionary<ITypeReference, int> _exportedTypeIndex;
        private readonly List<ITypeReference> _exportedTypeList;
        private readonly Dictionary<string, int> _fileRefIndex = new Dictionary<string, int>(32);  //more than enough in most cases
        private readonly List<IFileReference> _fileRefList = new List<IFileReference>(32);
        private readonly Dictionary<IFieldReference, BlobIdx> _fieldSignatureIndex = new Dictionary<IFieldReference, BlobIdx>();

        // We need to keep track of both the index of the signature and the actual blob to support VB static local naming scheme.
        private readonly Dictionary<ISignature, KeyValuePair<BlobIdx, ImmutableArray<byte>>> _signatureIndex;

        private readonly Dictionary<IMarshallingInformation, BlobIdx> _marshallingDescriptorIndex = new Dictionary<IMarshallingInformation, BlobIdx>();
        protected readonly List<MethodImplementation> methodImplList = new List<MethodImplementation>();
        private readonly Dictionary<IGenericMethodInstanceReference, BlobIdx> _methodInstanceSignatureIndex = new Dictionary<IGenericMethodInstanceReference, BlobIdx>();

        // Well known dummy cor library types whose refs are used for attaching assembly attributes off within net modules
        // There is no guarantee the types actually exist in a cor library
        internal static readonly string dummyAssemblyAttributeParentNamespace = "System.Runtime.CompilerServices";
        internal static readonly string dummyAssemblyAttributeParentName = "AssemblyAttributesGoHere";
        internal static readonly string[,] dummyAssemblyAttributeParentQualifier = { { "", "M" }, { "S", "SM" } };
        private readonly uint[,] _dummyAssemblyAttributeParent = { { 0, 0 }, { 0, 0 } };

        internal const int MappedFieldDataAlignment = 8;
        internal const int ManagedResourcesDataAlignment = 8;

        internal IModule Module => module;

        private void CreateMethodBodyReferenceIndex()
        {
            int count;
            var referencesInIL = module.ReferencesInIL(out count);

            _pseudoSymbolTokenToTokenMap = new int[count];
            _pseudoSymbolTokenToReferenceMap = new IReference[count];

            int cur = 0;
            foreach (IReference o in referencesInIL)
            {
                _pseudoSymbolTokenToReferenceMap[cur] = o;
                cur++;
            }
        }

        private void CreateIndices()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            this.CreateUserStringIndices();
            this.CreateInitialAssemblyRefIndex();
            this.CreateInitialFileRefIndex();
            this.CreateIndicesForModule();
            this.CreateInitialExportedTypeIndex();

            // Find all references and assign tokens.
            _referenceVisitor = this.CreateReferenceVisitor();
            this.module.Dispatch(_referenceVisitor);

            this.CreateMethodBodyReferenceIndex();
        }

        private void CreateUserStringIndices()
        {
            _pseudoStringTokenToStringMap = new List<string>();

            foreach (string str in this.module.GetStrings())
            {
                _pseudoStringTokenToStringMap.Add(str);
            }

            _pseudoStringTokenToTokenMap = new int[_pseudoStringTokenToStringMap.Count];
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
            _cancellationToken.ThrowIfCancellationRequested();

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
            Debug.Assert(!_tableIndicesAreComplete);
            foreach (IAssemblyReference assemblyRef in this.module.GetAssemblyReferences(Context))
            {
                this.GetOrAddAssemblyRefIndex(assemblyRef);
            }
        }

        private void CreateInitialExportedTypeIndex()
        {
            Debug.Assert(!_tableIndicesAreComplete);

            if (this.IsFullMetadata)
            {
                foreach (ITypeReference exportedType in this.module.GetExportedTypes(Context))
                {
                    if (!_exportedTypeIndex.ContainsKey(exportedType))
                    {
                        _exportedTypeList.Add(exportedType);
                        _exportedTypeIndex.Add(exportedType, _exportedTypeList.Count);
                    }
                }
            }
        }

        private void CreateInitialFileRefIndex()
        {
            Debug.Assert(!_tableIndicesAreComplete);
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return;
            }

            foreach (IFileReference fileRef in assembly.GetFiles(Context))
            {
                string key = fileRef.FileName;
                if (!_fileRefIndex.ContainsKey(key))
                {
                    _fileRefList.Add(fileRef);
                    _fileRefIndex.Add(key, _fileRefList.Count);
                }
            }
        }

        internal int GetAssemblyRefIndex(IAssemblyReference assemblyReference)
        {
            var containingAssembly = this.module.GetContainingAssembly(Context);

            if (containingAssembly != null && ReferenceEquals(assemblyReference, containingAssembly))
            {
                return 0;
            }

            return this.GetOrAddAssemblyRefIndex(assemblyReference);
        }

        internal int GetModuleRefIndex(string moduleName)
        {
            return this.GetOrAddModuleRefIndex(moduleName);
        }

        private BlobIdx GetCustomAttributeSignatureIndex(ICustomAttribute customAttribute)
        {
            BlobIdx result;
            if (_customAttributeSignatureIndex.TryGetValue(customAttribute, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeCustomAttributeSignature(customAttribute, false, writer);
            result = heaps.GetBlobIndex(writer);
            _customAttributeSignatureIndex.Add(customAttribute, result);
            writer.Free();
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

            return methodDef != null
                ? GetMethodDefIndex(methodDef).ToCodedIndex(CustomAttributeTypeTag.MethodDef)
                : GetMemberRefIndex(methodReference).ToCodedIndex(CustomAttributeTypeTag.MemberRef);
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

        private int GetExportedTypeIndex(ITypeReference typeReference)
        {
            int result;
            if (_exportedTypeIndex.TryGetValue(typeReference, out result))
            {
                return result;
            }

            Debug.Assert(!_tableIndicesAreComplete);
            _exportedTypeList.Add(typeReference);
            _exportedTypeIndex.Add(typeReference, _exportedTypeList.Count);
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

        internal BlobIdx GetFieldSignatureIndex(IFieldReference fieldReference)
        {
            BlobIdx result;
            ISpecializedFieldReference specializedFieldReference = fieldReference.AsSpecializedFieldReference;
            if (specializedFieldReference != null)
            {
                fieldReference = specializedFieldReference.UnspecializedVersion;
            }

            if (_fieldSignatureIndex.TryGetValue(fieldReference, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeFieldSignature(fieldReference, writer);
            result = heaps.GetBlobIndex(writer);
            _fieldSignatureIndex.Add(fieldReference, result);
            writer.Free();
            return result;
        }

        internal virtual int GetFieldToken(IFieldReference fieldReference)
        {
            IFieldDefinition fieldDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(fieldReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                fieldDef = fieldReference.GetResolvedField(Context);
            }

            return fieldDef != null
                ? 0x04000000 | this.GetFieldDefIndex(fieldDef)
                : 0x0A000000 | this.GetMemberRefIndex(fieldReference);
        }

        internal int GetFileRefIndex(IFileReference fileReference)
        {
            string key = fileReference.FileName;
            int result;
            if (_fileRefIndex.TryGetValue(key, out result))
            {
                return result;
            }

            Debug.Assert(!_tableIndicesAreComplete);
            _fileRefList.Add(fileReference);
            _fileRefIndex.Add(key, _fileRefList.Count);
            return result;
        }

        private int GetFileRefIndex(IModuleReference mref)
        {
            string key = mref.Name;
            int result;
            if (_fileRefIndex.TryGetValue(key, out result))
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
                return GetAssemblyRefIndex(aref).ToCodedIndex(ImplementationTag.AssemblyRef);
            }

            IModuleReference mref = uref as IModuleReference;
            if (mref != null)
            {
                aref = mref.GetContainingAssembly(Context);
                return aref == null || ReferenceEquals(aref, this.module.GetContainingAssembly(Context))
                    ? GetFileRefIndex(mref).ToCodedIndex(ImplementationTag.File)
                    : GetAssemblyRefIndex(aref).ToCodedIndex(ImplementationTag.AssemblyRef);
            }

            Debug.Assert(false);

            // TODO: error
            return 0;
        }

        private static uint GetManagedResourceOffset(ManagedResource resource, BlobBuilder resourceWriter)
        {
            if (resource.ExternalFile != null)
            {
                return resource.Offset;
            }

            int result = resourceWriter.Position;
            resource.WriteData(resourceWriter);
            return (uint)result;
        }

        public static string GetMangledName(INamedTypeReference namedType)
        {
            string unmangledName = namedType.Name;

            return namedType.MangleName
                ? MetadataHelpers.ComposeAritySuffixedMetadataName(unmangledName, namedType.GenericParameterCount)
                : unmangledName;
        }

        internal int GetMemberRefIndex(ITypeMemberReference memberRef)
        {
            return this.GetOrAddMemberRefIndex(memberRef);
        }

        internal uint GetMemberRefParentCodedIndex(ITypeMemberReference memberRef)
        {
            ITypeDefinition parentTypeDef = memberRef.GetContainingType(Context).AsTypeDefinition(Context);
            if (parentTypeDef != null)
            {
                int parentTypeDefIndex;
                this.TryGetTypeDefIndex(parentTypeDef, out parentTypeDefIndex);
                if (parentTypeDefIndex > 0)
                {
                    if (memberRef is IFieldReference)
                    {
                        return parentTypeDefIndex.ToCodedIndex(MemberRefParentTag.TypeDef);
                    }

                    IMethodReference methodRef = memberRef as IMethodReference;
                    if (methodRef != null)
                    {
                        if (methodRef.AcceptsExtraArguments)
                        {
                            int methodIndex;
                            if (this.TryGetMethodDefIndex(methodRef.GetResolvedMethod(Context), out methodIndex))
                            {
                                return methodIndex.ToCodedIndex(MemberRefParentTag.MethodDef);
                            }
                        }

                        return parentTypeDefIndex.ToCodedIndex(MemberRefParentTag.TypeDef);
                    }
                    // TODO: error
                }
            }

            // TODO: special treatment for global fields and methods. Object model support would be nice.
            return memberRef.GetContainingType(Context).IsTypeSpecification()
                ? GetTypeSpecIndex(memberRef.GetContainingType(Context)).ToCodedIndex(MemberRefParentTag.TypeSpec)
                : GetTypeRefIndex(memberRef.GetContainingType(Context)).ToCodedIndex(MemberRefParentTag.TypeRef);
        }

        internal uint GetMethodDefOrRefCodedIndex(IMethodReference methodReference)
        {
            IMethodDefinition methodDef = null;
            IUnitReference definingUnit = GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, this.module))
            {
                methodDef = methodReference.GetResolvedMethod(Context);
            }

            return methodDef != null
                ? GetMethodDefIndex(methodDef).ToCodedIndex(MethodDefOrRefTag.MethodDef)
                : GetMemberRefIndex(methodReference).ToCodedIndex(MethodDefOrRefTag.MemberRef);
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

        internal BlobIdx GetMethodInstanceSignatureIndex(IGenericMethodInstanceReference methodInstanceReference)
        {
            BlobIdx result;
            if (_methodInstanceSignatureIndex.TryGetValue(methodInstanceReference, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            writer.WriteByte(0x0A);
            writer.WriteCompressedInteger(methodInstanceReference.GetGenericMethod(Context).GenericParameterCount);
            foreach (ITypeReference typeref in methodInstanceReference.GetGenericArguments(Context))
            {
                this.SerializeTypeReference(typeref, writer, false, true);
            }

            result = heaps.GetBlobIndex(writer);
            _methodInstanceSignatureIndex.Add(methodInstanceReference, result);
            writer.Free();
            return result;
        }

        private BlobIdx GetMarshallingDescriptorIndex(IMarshallingInformation marshallingInformation)
        {
            BlobIdx result;
            if (_marshallingDescriptorIndex.TryGetValue(marshallingInformation, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeMarshallingDescriptor(marshallingInformation, writer);
            result = heaps.GetBlobIndex(writer);
            _marshallingDescriptorIndex.Add(marshallingInformation, result);
            writer.Free();
            return result;
        }

        private BlobIdx GetMarshallingDescriptorIndex(ImmutableArray<byte> descriptor)
        {
            return heaps.GetBlobIndex(descriptor);
        }

        private BlobIdx GetMemberRefSignatureIndex(ITypeMemberReference memberRef)
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
            }

            // TODO: error
            return default(BlobIdx);
        }

        internal BlobIdx GetMethodSignatureIndex(IMethodReference methodReference)
        {
            ImmutableArray<byte> signatureBlob;
            return GetMethodSignatureIndexAndBlob(methodReference, out signatureBlob);
        }

        internal byte[] GetMethodSignature(IMethodReference methodReference)
        {
            ImmutableArray<byte> signatureBlob;
            GetMethodSignatureIndexAndBlob(methodReference, out signatureBlob);
            return signatureBlob.ToArray();
        }

        private BlobIdx GetMethodSignatureIndexAndBlob(IMethodReference methodReference, out ImmutableArray<byte> signatureBlob)
        {
            BlobIdx result;
            ISpecializedMethodReference specializedMethodReference = methodReference.AsSpecializedMethodReference;
            if (specializedMethodReference != null)
            {
                methodReference = specializedMethodReference.UnspecializedVersion;
            }

            KeyValuePair<BlobIdx, ImmutableArray<byte>> existing;
            if (_signatureIndex.TryGetValue(methodReference, out existing))
            {
                signatureBlob = existing.Value;
                return existing.Key;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeSignature(methodReference, methodReference.GenericParameterCount, methodReference.ExtraParameters, writer);

            signatureBlob = writer.ToImmutableArray();
            result = heaps.GetBlobIndex(signatureBlob);
            _signatureIndex.Add(methodReference, KeyValuePair.Create(result, signatureBlob));
            writer.Free();
            return result;
        }

        private BlobIdx GetGenericMethodInstanceIndex(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeGenericMethodInstanceSignature(writer, genericMethodInstanceReference);
            BlobIdx result = heaps.GetBlobIndex(writer);
            writer.Free();
            return result;
        }

        private int GetMethodSpecIndex(IGenericMethodInstanceReference methodSpec)
        {
            return this.GetOrAddMethodSpecIndex(methodSpec);
        }

        internal virtual int GetMethodToken(IMethodReference methodReference)
        {
            int methodDefIndex;
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

            IGenericMethodInstanceReference methodSpec = methodReference.AsGenericMethodInstanceReference;
            return methodSpec != null
                ? 0x2B000000 | this.GetMethodSpecIndex(methodSpec)
                : 0x0A000000 | this.GetMemberRefIndex(methodReference);
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

        private BlobIdx GetPermissionSetIndex(ImmutableArray<ICustomAttribute> permissionSet)
        {
            var writer = PooledBlobBuilder.GetInstance();
            BlobIdx result;
            try
            {
                writer.WriteByte((byte)'.');
                writer.WriteCompressedInteger((uint)permissionSet.Length);
                this.SerializePermissionSet(permissionSet, writer);
                result = heaps.GetBlobIndex(writer);
            }
            finally
            {
                writer.Free();
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

        private BlobIdx GetPropertySignatureIndex(IPropertyDefinition propertyDef)
        {
            KeyValuePair<BlobIdx, ImmutableArray<byte>> existing;
            if (_signatureIndex.TryGetValue(propertyDef, out existing))
            {
                return existing.Key;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeSignature(propertyDef, 0, ImmutableArray<IParameterTypeInformation>.Empty, writer);
            var blob = writer.ToImmutableArray();
            var result = heaps.GetBlobIndex(blob);
            _signatureIndex.Add(propertyDef, KeyValuePair.Create(result, blob));
            writer.Free();
            return result;
        }

        private uint GetResolutionScopeCodedIndex(IUnitReference unitReference)
        {
            IAssemblyReference aref = unitReference as IAssemblyReference;
            if (aref != null)
            {
                return GetAssemblyRefIndex(aref).ToCodedIndex(ResolutionScopeTag.AssemblyRef);
            }

            IModuleReference mref = unitReference as IModuleReference;
            if (mref != null)
            {
                // If this is a module from a referenced multi-module assembly,
                // the assembly should be used as the resolution scope.
                aref = mref.GetContainingAssembly(Context);

                if (aref != null && aref != module.AsAssembly)
                {
                    return GetAssemblyRefIndex(aref).ToCodedIndex(ResolutionScopeTag.AssemblyRef);
                }

                return GetModuleRefIndex(mref.Name).ToCodedIndex(ResolutionScopeTag.ModuleRef);
            }

            // TODO: error
            return 0;
        }

        private StringIdx GetStringIndexForPathAndCheckLength(string path, INamedEntity errorEntity = null)
        {
            CheckPathLength(path, errorEntity);
            return heaps.GetStringIndex(path);
        }

        private StringIdx GetStringIndexForNameAndCheckLength(string name, INamedEntity errorEntity = null)
        {
            CheckNameLength(name, errorEntity);
            return heaps.GetStringIndex(name);
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
                return default(StringIdx);
            }

            CheckNamespaceLength(namespaceName, mangledTypeName, namespaceType);
            return heaps.GetStringIndex(namespaceName);
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
                    s_utf8Encoding.GetByteCount(namespaceName) +
                    1 + // dot
                    s_utf8Encoding.GetByteCount(mangledTypeName);

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

            int utf8Length = s_utf8Encoding.GetByteCount(str);
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
            int typeDefIndex;
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            if ((typeDefinition != null) && this.TryGetTypeDefIndex(typeDefinition, out typeDefIndex))
            {
                return typeDefIndex.ToCodedIndex(TypeDefOrRefTag.TypeDef);
            }

            return treatRefAsPotentialTypeSpec && typeReference.IsTypeSpecification()
                ? GetTypeSpecIndex(typeReference).ToCodedIndex(TypeDefOrRefTag.TypeSpec)
                : GetTypeRefIndex(typeReference).ToCodedIndex(TypeDefOrRefTag.TypeRef);
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
                return GetTypeDefIndex(genTypePar.DefiningType).ToCodedIndex(TypeOrMethodDefTag.TypeDef);
            }

            IGenericMethodParameter genMethPar = genPar.AsGenericMethodParameter;
            if (genMethPar != null)
            {
                return GetMethodDefIndex(genMethPar.DefiningMethod).ToCodedIndex(TypeOrMethodDefTag.MethodDef);
            }

            // TODO: error
            return 0;
        }

        private int GetTypeRefIndex(ITypeReference typeReference)
        {
            int result;
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

        private int GetTypeSpecIndex(ITypeReference typeReference)
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

        internal BlobIdx GetTypeSpecSignatureIndex(ITypeReference typeReference)
        {
            BlobIdx result;
            if (_typeSpecSignatureIndex.TryGetValue(typeReference, out result))
            {
                return result;
            }

            var writer = PooledBlobBuilder.GetInstance();
            this.SerializeTypeReference(typeReference, writer, false, true);
            result = heaps.GetBlobIndex(writer);
            _typeSpecSignatureIndex.Add(typeReference, result);
            writer.Free();
            return result;
        }

        internal void RecordTypeReference(ITypeReference typeReference)
        {
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            int token;
            if ((typeDefinition != null) && this.TryGetTypeDefIndex(typeDefinition, out token))
            {
                return;
            }

            if (!typeReference.IsTypeSpecification())
            {
                this.GetTypeRefIndex(typeReference);
            }
            else
            {
                this.GetTypeSpecIndex(typeReference);
            }
        }

        internal virtual int GetTypeToken(ITypeReference typeReference)
        {
            int typeDefIndex;
            var typeDefinition = typeReference.AsTypeDefinition(this.Context);
            if (typeDefinition != null && this.TryGetTypeDefIndex(typeDefinition, out typeDefIndex))
            {
                return 0x02000000 | typeDefIndex;
            }

            return typeReference.IsTypeSpecification()
                ? 0x1B000000 | this.GetTypeSpecIndex(typeReference)
                : 0x01000000 | this.GetTypeRefIndex(typeReference);
        }

        internal int GetTokenForDefinition(IDefinition definition)
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

        private void SerializeCustomModifiers(ImmutableArray<ICustomModifier> customModifiers, BlobBuilder writer)
        {
            foreach (ICustomModifier customModifier in customModifiers)
            {
                this.SerializeCustomModifier(customModifier, writer);
            }
        }

        private void SerializeCustomModifier(ICustomModifier customModifier, BlobBuilder writer)
        {
            if (customModifier.IsOptional)
            {
                writer.WriteByte(0x20);
            }
            else
            {
                writer.WriteByte(0x1f);
            }

            writer.WriteCompressedInteger(this.GetTypeDefOrRefCodedIndex(customModifier.GetModifier(Context), true));
        }

        public void WriteMetadataAndIL(PdbWriter pdbWriterOpt, Stream metadataStream, Stream ilStream, out MetadataSizes metadataSizes)
        {
            pdbWriterOpt?.SetMetadataEmitter(this);

            // TODO: we can precalculate the exact size of IL stream
            var ilWriter = new BlobBuilder(1024);
            var metadataWriter = new BlobBuilder(4 * 1024);
            var mappedFieldDataWriter = new BlobBuilder(0);
            var managedResourceDataWriter = new BlobBuilder(0);

            // Add 4B of padding to the start of the separated IL stream, 
            // so that method RVAs, which are offsets to this stream, are never 0.
            ilWriter.WriteUInt32(0);

            // this is used to handle edit-and-continue emit, so we should have a module
            // version ID that is imposed by the caller (the same as the previous module version ID).
            // Therefore we do not have to fill in a new module version ID in the generated metadata
            // stream.
            Debug.Assert(this.module.Properties.PersistentIdentifier != default(Guid));

            int moduleVersionIdOffsetInMetadataStream;
            int pdbIdOffsetInMetadataStream;
            int entryPointToken;

            PESizes peSizes; 
            SerializeMetadataAndIL(
                metadataWriter,
                default(BlobBuilder),
                pdbWriterOpt,
                ilWriter,
                mappedFieldDataWriter,
                managedResourceDataWriter,
                methodBodyStreamRva: 0,
                calculateMappedFieldDataStreamRva: _ => 0,
                moduleVersionIdOffsetInMetadataStream: out moduleVersionIdOffsetInMetadataStream,
                pdbIdOffsetInPortablePdbStream: out pdbIdOffsetInMetadataStream,
                peSizes: out peSizes,
                metadataSizes: out metadataSizes,
                entryPointToken: out entryPointToken);

            ilWriter.WriteContentTo(ilStream);
            metadataWriter.WriteContentTo(metadataStream);

            Debug.Assert(entryPointToken == 0);
            Debug.Assert(mappedFieldDataWriter.Count == 0);
            Debug.Assert(managedResourceDataWriter.Count == 0);
            Debug.Assert(pdbIdOffsetInMetadataStream == 0);
        }

        public void SerializeMetadataAndIL(
            BlobBuilder metadataWriter,
            BlobBuilder debugMetadataWriterOpt,
            PdbWriter nativePdbWriterOpt,
            BlobBuilder ilWriter,
            BlobBuilder mappedFieldDataWriter,
            BlobBuilder managedResourceDataWriter,
            int methodBodyStreamRva,
            Func<PESizes, int> calculateMappedFieldDataStreamRva,
            out int moduleVersionIdOffsetInMetadataStream,
            out int pdbIdOffsetInPortablePdbStream,
            out int entryPointToken,
            out PESizes peSizes,
            out MetadataSizes metadataSizes)
        {
            // Extract information from object model into tables, indices and streams
            CreateIndices();

            if (_debugHeapsOpt != null)
            {
                DefineModuleImportScope();
            }

            int[] methodBodyRvas = SerializeMethodBodies(ilWriter, nativePdbWriterOpt);

            _cancellationToken.ThrowIfCancellationRequested();

            // method body serialization adds Stand Alone Signatures
            _tableIndicesAreComplete = true;

            ReportReferencesToAddedSymbols();

            PopulateTables(methodBodyRvas, mappedFieldDataWriter, managedResourceDataWriter);

            int debugEntryPointToken;
            if (IsFullMetadata)
            {
                // PE entry point is set for executable programs
                IMethodReference entryPoint = module.PEEntryPoint;
                entryPointToken = entryPoint != null ? GetMethodToken((IMethodDefinition)entryPoint.AsDefinition(Context)) : 0;

                // debug entry point may be different from PE entry point, it may also be set for libraries
                IMethodReference debugEntryPoint = module.DebugEntryPoint;
                if (debugEntryPoint != null && debugEntryPoint != entryPoint)
                {
                    debugEntryPointToken = GetMethodToken((IMethodDefinition)debugEntryPoint.AsDefinition(Context));
                }
                else
                {
                    debugEntryPointToken = entryPointToken;
                }

                // entry point can only be a MethodDef:
                Debug.Assert(entryPointToken == 0 || (entryPointToken & 0xff000000) == 0x06000000);
                Debug.Assert(debugEntryPointToken == 0 || (debugEntryPointToken & 0xff000000) == 0x06000000);

                if (debugEntryPointToken != 0)
                {
                    nativePdbWriterOpt?.SetEntryPoint((uint)debugEntryPointToken);
                }
            }
            else
            {
                entryPointToken = debugEntryPointToken = 0;
            }

            heaps.Complete();

            var tableRowCounts = GetRowCounts();

            metadataSizes = new MetadataSizes(
                rowCounts: tableRowCounts,
                heapSizes: heaps.GetHeapSizes(),
                isMinimalDelta: IsMinimalDelta,
                emitStandaloneDebugMetadata: EmitStandaloneDebugMetadata,
                isStandaloneDebugMetadata: false);

            peSizes = new PESizes(
                metadataSizes.MetadataSize,
                ilStreamSize: ilWriter.Count,
                mappedFieldDataSize: mappedFieldDataWriter.Count,
                resourceDataSize: managedResourceDataWriter.Count,
                strongNameSignatureSize: CalculateStrongNameSignatureSize(module));

            int mappedFieldDataStreamRva = calculateMappedFieldDataStreamRva(peSizes);

            int guidHeapStartOffset;
            SerializeMetadata(metadataWriter, metadataSizes, methodBodyStreamRva, mappedFieldDataStreamRva, debugEntryPointToken, out guidHeapStartOffset, out pdbIdOffsetInPortablePdbStream);
            moduleVersionIdOffsetInMetadataStream = GetModuleVersionGuidOffsetInMetadataStream(guidHeapStartOffset);
            Debug.Assert(pdbIdOffsetInPortablePdbStream == 0);

            if (!EmitStandaloneDebugMetadata)
            {
                return;
            }

            // serialize debug metadata stream

            Debug.Assert(_debugHeapsOpt != null);
            _debugHeapsOpt.Complete();

            var debugMetadataSizes = new MetadataSizes(
                rowCounts: tableRowCounts,
                heapSizes: _debugHeapsOpt.GetHeapSizes(),
                isMinimalDelta: IsMinimalDelta,
                emitStandaloneDebugMetadata: true,
                isStandaloneDebugMetadata: true);

            SerializeMetadata(debugMetadataWriterOpt, debugMetadataSizes, 0, 0, debugEntryPointToken, out guidHeapStartOffset, out pdbIdOffsetInPortablePdbStream);
        }

        private static int CalculateStrongNameSignatureSize(IModule module)
        {
            IAssembly assembly = module.AsAssembly;
            if (assembly == null)
            {
                return 0;
            }

            // EDMAURER the count of characters divided by two because the each pair of characters will turn in to one byte.
            int keySize = (assembly.SignatureKey == null) ? 0 : assembly.SignatureKey.Length / 2;

            if (keySize == 0)
            {
                keySize = assembly.PublicKey.Length;
            }

            if (keySize == 0)
            {
                return 0;
            }

            return (keySize < 128 + 32) ? 128 : keySize - 32;
        }

        private int GetModuleVersionGuidOffsetInMetadataStream(int guidHeapOffsetInMetadataStream)
        {
            // index of module version ID in the guidWriter stream
            int moduleVersionIdIndex = _moduleRow.ModuleVersionId;

            // offset into the guidWriter stream of the module version ID
            int moduleVersionOffsetInGuidTable = (moduleVersionIdIndex - 1) << 4;

            return guidHeapOffsetInMetadataStream + moduleVersionOffsetInGuidTable;
        }

        private void PopulateTables(int[] methodBodyRvas, BlobBuilder mappedFieldDataWriter, BlobBuilder resourceWriter)
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
            this.PopulateModuleTableRow();
            this.PopulateNestedClassTableRows();
            this.PopulateParamTableRows();
            this.PopulatePropertyMapTableRows();
            this.PopulatePropertyTableRows();
            this.PopulateTypeDefTableRows();
            this.PopulateTypeRefTableRows();
            this.PopulateTypeSpecTableRows();
            this.PopulateStandaloneSignatures();

            // This table is populated after the others because it depends on the order of the entries of the generic parameter table.
            this.PopulateCustomAttributeTableRows();

            ImmutableArray<int> rowCounts = GetRowCounts();
            Debug.Assert(rowCounts[(int)TableIndex.EncLog] == 0 && rowCounts[(int)TableIndex.EncMap] == 0);

            this.PopulateEncLogTableRows(_encLogTable, rowCounts);
            this.PopulateEncMapTableRows(_encMapTable, rowCounts);
        }

        private void PopulateAssemblyRefTableRows()
        {
            var assemblyRefs = this.GetAssemblyRefs();
            _assemblyRefTable.Capacity = assemblyRefs.Count;

            foreach (var assemblyRef in assemblyRefs)
            {
                Debug.Assert(assemblyRef.Version != null);
                Debug.Assert(!string.IsNullOrEmpty(assemblyRef.Name));

                AssemblyRefTableRow r = new AssemblyRefTableRow();
                r.Version = assemblyRef.Version;
                r.PublicKeyToken = heaps.GetBlobIndex(assemblyRef.PublicKeyToken);
                r.Name = this.GetStringIndexForPathAndCheckLength(assemblyRef.Name, assemblyRef);
                r.Culture = heaps.GetStringIndex(assemblyRef.Culture);

                // reference has token, not full public key
                r.Flags = ((uint)assemblyRef.ContentType << 9) | (assemblyRef.IsRetargetable ? (uint)AssemblyFlags.Retargetable : 0U);
                r.HashValue = default(BlobIdx);

                _assemblyRefTable.Add(r);
            }
        }

        /// <summary>
        /// Compares quality of assembly references to achieve unique rows in AssemblyRef table.
        /// Metadata spec: "The AssemblyRef table shall contain no duplicates (where duplicate rows are deemed to 
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
                    ByteSequenceComparer.Equals(x.PublicKeyToken, y.PublicKeyToken) &&
                    x.Name == y.Name &&
                    x.Culture == y.Culture;
            }

            public int GetHashCode(IAssemblyReference reference)
            {
                return Hash.Combine(reference.Version,
                       Hash.Combine(ByteSequenceComparer.GetHashCode(reference.PublicKeyToken),
                       Hash.Combine(reference.Name.GetHashCode(),
                       Hash.Combine(reference.Culture, 0))));
            }
        }

        private void PopulateAssemblyTableRows()
        {
            if (!EmitAssemblyDefinition)
            {
                return;
            }

            IAssembly assembly = this.module.AsAssembly;

            _assemblyTable.Add(new AssemblyRow
            {
                Flags = (ushort)assembly.Flags,
                HashAlgorithm = (uint)assembly.HashAlgorithm,
                Version = assembly.Version,
                AssemblyKey = heaps.GetBlobIndex(assembly.PublicKey),
                AssemblyName = GetStringIndexForPathAndCheckLength(assembly.Name, assembly),
                AssemblyCulture = heaps.GetStringIndex(assembly.Culture)
            });
        }

        private void PopulateClassLayoutTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                if (typeDef.Alignment == 0 && typeDef.SizeOf == 0)
                {
                    continue;
                }

                ClassLayoutRow r = new ClassLayoutRow();
                r.PackingSize = typeDef.Alignment;
                r.ClassSize = typeDef.SizeOf;
                r.Parent = (uint)this.GetTypeDefIndex(typeDef);
                _classLayoutTable.Add(r);
            }
        }

        private void PopulateConstantTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                var constant = fieldDef.GetCompileTimeValue(Context);
                if (constant == null)
                {
                    continue;
                }

                int fieldDefIndex = GetFieldDefIndex(fieldDef);
                _constantTable.Add(CreateConstantRow(constant.Value, parent: fieldDefIndex.ToCodedIndex(HasConstantTag.Field)));
            }

            int sizeWithOnlyFields = _constantTable.Count;
            foreach (IParameterDefinition parDef in this.GetParameterDefs())
            {
                var defaultValue = parDef.GetDefaultValue(Context);
                if (defaultValue == null)
                {
                    continue;
                }

                int parameterDefIndex = GetParameterDefIndex(parDef);
                _constantTable.Add(CreateConstantRow(defaultValue.Value, parent: parameterDefIndex.ToCodedIndex(HasConstantTag.Param)));
            }

            foreach (IPropertyDefinition propDef in this.GetPropertyDefs())
            {
                if (!propDef.HasDefaultValue)
                {
                    continue;
                }

                int propertyDefIndex = GetPropertyDefIndex(propDef);
                _constantTable.Add(CreateConstantRow(propDef.DefaultValue.Value, parent: propertyDefIndex.ToCodedIndex(HasConstantTag.Property)));
            }

            if (sizeWithOnlyFields > 0 && sizeWithOnlyFields < _constantTable.Count)
            {
                _constantTable.Sort(new ConstantRowComparer());
            }
        }

        private class ConstantRowComparer : Comparer<ConstantRow>
        {
            public override int Compare(ConstantRow x, ConstantRow y)
            {
                return ((int)x.Parent) - (int)y.Parent;
            }
        }

        private ConstantRow CreateConstantRow(object value, uint parent)
        {
            return new ConstantRow
            {
                Type = (byte)MetadataUtilities.GetConstantTypeCode(value),
                Parent = parent,
                Value = heaps.GetConstantBlobIndex(value)
            };
        }

        private void PopulateCustomAttributeTableRows()
        {
            if (this.IsFullMetadata)
            {
                this.AddAssemblyAttributesToTable();
            }

            this.AddCustomAttributesToTable(this.GetMethodDefs(), HasCustomAttributeTag.MethodDef, this.GetMethodDefIndex);
            this.AddCustomAttributesToTable(this.GetFieldDefs(), HasCustomAttributeTag.Field, this.GetFieldDefIndex);

            // this.AddCustomAttributesToTable(this.typeRefList, 2);
            this.AddCustomAttributesToTable(this.GetTypeDefs(), HasCustomAttributeTag.TypeDef, this.GetTypeDefIndex);
            this.AddCustomAttributesToTable(this.GetParameterDefs(), HasCustomAttributeTag.Param, this.GetParameterDefIndex);

            // TODO: attributes on interface implementation entries 5
            // TODO: attributes on member reference entries 6
            if (this.IsFullMetadata)
            {
                this.AddModuleAttributesToTable(this.module, HasCustomAttributeTag.Module);
            }

            // TODO: declarative security entries 8
            this.AddCustomAttributesToTable(this.GetPropertyDefs(), HasCustomAttributeTag.Property, this.GetPropertyDefIndex);
            this.AddCustomAttributesToTable(this.GetEventDefs(), HasCustomAttributeTag.Event, this.GetEventDefIndex);

            // TODO: standalone signature entries 11
            if (this.IsFullMetadata)
            {
                this.AddCustomAttributesToTable(this.module.ModuleReferences, HasCustomAttributeTag.ModuleRef);
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
            foreach (GenericParamRow genericParamRow in _genericParamTable)
            {
                sortedGenericParameterList.Add(genericParamRow.GenericParameter);
            }

            this.AddCustomAttributesToTable(sortedGenericParameterList, HasCustomAttributeTag.GenericParam);

            _customAttributeTable.Sort(new CustomAttributeRowComparer());
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
                    true,               // needsDummyParent
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
            uint parentToken = 1.ToCodedIndex(HasCustomAttributeTag.Assembly);
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
            if (_dummyAssemblyAttributeParent[iS, iM] == 0)
            {
                TypeRefRow r = new TypeRefRow();
                r.ResolutionScope = this.GetResolutionScopeCodedIndex(this.module.GetCorLibrary(Context));
                r.Name = heaps.GetStringIndex(dummyAssemblyAttributeParentName + dummyAssemblyAttributeParentQualifier[iS, iM]);
                r.Namespace = heaps.GetStringIndex(dummyAssemblyAttributeParentNamespace);
                _typeRefTable.Add(r);
                _dummyAssemblyAttributeParent[iS, iM] = _typeRefTable.Count.ToCodedIndex(HasCustomAttributeTag.TypeRef);
            }
            return _dummyAssemblyAttributeParent[iS, iM];
        }

        private void AddModuleAttributesToTable(IModule module, HasCustomAttributeTag tag)
        {
            Debug.Assert(this.IsFullMetadata); // parentToken is not relative
            uint parentToken = 1.ToCodedIndex(tag);
            foreach (ICustomAttribute customAttribute in module.ModuleAttributes)
            {
                AddCustomAttributeToTable(parentToken, customAttribute);
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, HasCustomAttributeTag tag)
            where T : IReference
        {
            int parentIndex = 0;
            foreach (var parent in parentList)
            {
                parentIndex++;
                uint parentToken = parentIndex.ToCodedIndex(tag);
                foreach (ICustomAttribute customAttribute in parent.GetAttributes(Context))
                {
                    AddCustomAttributeToTable(parentToken, customAttribute);
                }
            }
        }

        private void AddCustomAttributesToTable<T>(IEnumerable<T> parentList, HasCustomAttributeTag tag, Func<T, int> getDefIndex)
            where T : IReference
        {
            foreach (var parent in parentList)
            {
                uint parentToken = getDefIndex(parent).ToCodedIndex(tag);
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
            r.OriginalPosition = _customAttributeTable.Count;
            _customAttributeTable.Add(r);
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

        private void PopulateDeclSecurityTableRows()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly != null)
            {
                this.PopulateDeclSecurityTableRowsFor(1, HasDeclSecurityTag.Assembly, assembly.AssemblySecurityAttributes);
            }

            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                if (!typeDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                this.PopulateDeclSecurityTableRowsFor(GetTypeDefIndex(typeDef), HasDeclSecurityTag.TypeDef, typeDef.SecurityAttributes);
            }

            foreach (IMethodDefinition methodDef in this.GetMethodDefs())
            {
                if (!methodDef.HasDeclarativeSecurity)
                {
                    continue;
                }

                this.PopulateDeclSecurityTableRowsFor(GetMethodDefIndex(methodDef), HasDeclSecurityTag.MethodDef, methodDef.SecurityAttributes);
            }

            _declSecurityTable.Sort(new DeclSecurityRowComparer());
        }

        private void PopulateDeclSecurityTableRowsFor(int parentIndex, HasDeclSecurityTag tag, IEnumerable<SecurityAttribute> attributes)
        {
            OrderPreservingMultiDictionary<DeclarativeSecurityAction, ICustomAttribute> groupedSecurityAttributes = null;

            foreach (SecurityAttribute securityAttribute in attributes)
            {
                groupedSecurityAttributes = groupedSecurityAttributes ?? OrderPreservingMultiDictionary<DeclarativeSecurityAction, ICustomAttribute>.GetInstance();
                groupedSecurityAttributes.Add(securityAttribute.Action, securityAttribute.Attribute);
            }

            if (groupedSecurityAttributes == null)
            {
                return;
            }

            DeclSecurityRow r = new DeclSecurityRow();
            r.Parent = parentIndex.ToCodedIndex(tag);

            foreach (DeclarativeSecurityAction securityAction in groupedSecurityAttributes.Keys)
            {
                r.Action = (ushort)securityAction;
                r.PermissionSet = this.GetPermissionSetIndex(groupedSecurityAttributes[securityAction]);
                r.OriginalIndex = _declSecurityTable.Count;
                _declSecurityTable.Add(r);
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

        private void PopulateEventMapTableRows()
        {
            this.PopulateEventMapTableRows(_eventMapTable);
        }

        private void PopulateEventTableRows()
        {
            var eventDefs = this.GetEventDefs();
            _eventTable.Capacity = eventDefs.Count;

            foreach (IEventDefinition eventDef in eventDefs)
            {
                EventRow r = new EventRow();
                r.EventFlags = GetEventFlags(eventDef);
                r.Name = this.GetStringIndexForNameAndCheckLength(eventDef.Name, eventDef);
                r.EventType = this.GetTypeDefOrRefCodedIndex(eventDef.GetType(Context), true);
                _eventTable.Add(r);
            }
        }

        private void PopulateExportedTypeTableRows()
        {
            if (this.IsFullMetadata)
            {
                _exportedTypeTable.Capacity = this.NumberOfTypeDefsEstimate;

                foreach (ITypeReference exportedType in this.module.GetExportedTypes(Context))
                {
                    INestedTypeReference nestedRef;
                    INamespaceTypeReference namespaceTypeRef;
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
                        r.TypeNamespace = default(StringIdx);

                        var containingType = nestedRef.GetContainingType(Context);
                        int ci = GetExportedTypeIndex(containingType);
                        r.Implementation = ci.ToCodedIndex(ImplementationTag.ExportedType);

                        var parentFlags = _exportedTypeTable[ci - 1].Flags;
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

                        var topLevelFlags = _exportedTypeTable[(int)this.GetExportedTypeIndex(topLevelType) - 1].Flags;
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

                    _exportedTypeTable.Add(r);
                }
            }
        }

        private void PopulateFieldLayoutTableRows()
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (fieldDef.ContainingTypeDefinition.Layout != LayoutKind.Explicit || fieldDef.IsStatic)
                {
                    continue;
                }

                uint fieldDefIndex = (uint)this.GetFieldDefIndex(fieldDef);
                FieldLayoutRow r = new FieldLayoutRow();
                r.Offset = (uint)fieldDef.Offset;
                r.Field = fieldDefIndex;
                _fieldLayoutTable.Add(r);
            }
        }

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

                r.NativeType = (marshallingInformation != null)
                    ? GetMarshallingDescriptorIndex(marshallingInformation)
                    : GetMarshallingDescriptorIndex(fieldDef.MarshallingDescriptor);

                r.Parent = GetFieldDefIndex(fieldDef).ToCodedIndex(HasFieldMarshalTag.Field);
                _fieldMarshalTable.Add(r);
            }

            int sizeWithOnlyFields = _fieldMarshalTable.Count;
            foreach (IParameterDefinition parDef in this.GetParameterDefs())
            {
                if (!parDef.IsMarshalledExplicitly)
                {
                    continue;
                }

                FieldMarshalRow r = new FieldMarshalRow();

                var marshallingInformation = parDef.MarshallingInformation;

                r.NativeType = (marshallingInformation != null)
                    ? GetMarshallingDescriptorIndex(marshallingInformation)
                    : GetMarshallingDescriptorIndex(parDef.MarshallingDescriptor);

                r.Parent = GetParameterDefIndex(parDef).ToCodedIndex(HasFieldMarshalTag.Param);
                _fieldMarshalTable.Add(r);
            }

            if (sizeWithOnlyFields > 0 && sizeWithOnlyFields < _fieldMarshalTable.Count)
            {
                _fieldMarshalTable.Sort(new FieldMarshalRowComparer());
            }
        }

        private class FieldMarshalRowComparer : Comparer<FieldMarshalRow>
        {
            public override int Compare(FieldMarshalRow x, FieldMarshalRow y)
            {
                return ((int)x.Parent) - (int)y.Parent;
            }
        }

        private void PopulateFieldRvaTableRows(BlobBuilder mappedFieldDataWriter)
        {
            foreach (IFieldDefinition fieldDef in this.GetFieldDefs())
            {
                if (fieldDef.MappedData.IsDefault)
                {
                    continue;
                }

                uint fieldIndex = (uint)this.GetFieldDefIndex(fieldDef);
                FieldRvaRow r = new FieldRvaRow();

                r.Offset = (uint)mappedFieldDataWriter.Position;
                mappedFieldDataWriter.WriteBytes(fieldDef.MappedData);
                mappedFieldDataWriter.Align(MappedFieldDataAlignment);

                r.Field = fieldIndex;
                _fieldRvaTable.Add(r);
            }
        }

        private void PopulateFieldTableRows()
        {
            var fieldDefs = this.GetFieldDefs();
            _fieldDefTable.Capacity = fieldDefs.Count;

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
                _fieldDefTable.Add(r);
            }
        }

        private void PopulateFileTableRows()
        {
            IAssembly assembly = this.module.AsAssembly;
            if (assembly == null)
            {
                return;
            }

            var hashAlgorithm = assembly.HashAlgorithm;
            _fileTable.Capacity = _fileRefList.Count;

            foreach (IFileReference fileReference in _fileRefList)
            {
                FileTableRow r = new FileTableRow();
                r.Flags = fileReference.HasMetadata ? 0u : 1u;
                r.FileName = this.GetStringIndexForPathAndCheckLength(fileReference.FileName);
                r.HashValue = heaps.GetBlobIndex(fileReference.GetHashValue(hashAlgorithm));
                _fileTable.Add(r);
            }
        }

        private void PopulateGenericParamConstraintTableRows()
        {
            uint genericParamIndex = 0;
            foreach (GenericParamRow genericParameterRow in _genericParamTable)
            {
                genericParamIndex++;
                GenericParamConstraintRow r = new GenericParamConstraintRow();
                r.Owner = genericParamIndex;
                foreach (ITypeReference constraint in genericParameterRow.GenericParameter.GetConstraints(Context))
                {
                    r.Constraint = this.GetTypeDefOrRefCodedIndex(constraint, true);
                    _genericParamConstraintTable.Add(r);
                }
            }
        }
        
        private void PopulateGenericParamTableRows()
        {
            var genericParameters = this.GetGenericParameters();
            _genericParamTable.Capacity = genericParameters.Count;

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
                _genericParamTable.Add(r);
            }

            _genericParamTable.Sort(new GenericParamRowComparer());
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

                return x.Number - y.Number;
            }
        }

        private void PopulateImplMapTableRows()
        {
            foreach (IMethodDefinition methodDef in this.GetMethodDefs())
            {
                if (!methodDef.IsPlatformInvoke)
                {
                    continue;
                }

                var data = methodDef.PlatformInvokeData;
                var r = new ImplMapRow();
                r.MappingFlags = (ushort)data.Flags;
                r.MemberForwarded = GetMethodDefIndex(methodDef).ToCodedIndex(MemberForwardedTag.MethodDef);

                string entryPointName = data.EntryPointName;
                r.ImportName = entryPointName != null
                    ? this.GetStringIndexForNameAndCheckLength(entryPointName, methodDef)
                    : heaps.GetStringIndex(methodDef.Name); // Length checked while populating the method def table.

                r.ImportScope = (uint)this.GetModuleRefIndex(data.ModuleName);
                _implMapTable.Add(r);
            }
        }

        private void PopulateInterfaceImplTableRows()
        {
            foreach (ITypeDefinition typeDef in this.GetTypeDefs())
            {
                uint typeDefIndex = (uint)this.GetTypeDefIndex(typeDef);
                foreach (ITypeReference interfaceRef in typeDef.Interfaces(Context))
                {
                    InterfaceImplRow r = new InterfaceImplRow();
                    r.Class = typeDefIndex;
                    r.Interface = this.GetTypeDefOrRefCodedIndex(interfaceRef, true);
                    _interfaceImplTable.Add(r);
                }
            }
        }
        
        private void PopulateManifestResourceTableRows(BlobBuilder resourceDataWriter)
        {
            foreach (var resource in this.module.GetResources(Context))
            {
                ManifestResourceRow r = new ManifestResourceRow();
                r.Offset = GetManagedResourceOffset(resource, resourceDataWriter);
                r.Flags = resource.IsPublic ? 1u : 2u;
                r.Name = this.GetStringIndexForNameAndCheckLength(resource.Name);

                if (resource.ExternalFile != null)
                {
                    IFileReference externalFile = resource.ExternalFile;
                    // Length checked on insertion into the file table.
                    r.Implementation = GetFileRefIndex(externalFile).ToCodedIndex(ImplementationTag.File);
                }
                else
                {
                    // This is an embedded resource, we don't support references to resources from referenced assemblies.
                    r.Implementation = 0;
                }

                _manifestResourceTable.Add(r);
            }

            // the stream should be aligned:
            Debug.Assert((resourceDataWriter.Count % ManagedResourcesDataAlignment) == 0);
        }

        private void PopulateMemberRefTableRows()
        {
            var memberRefs = this.GetMemberRefs();
            _memberRefTable.Capacity = memberRefs.Count;

            foreach (ITypeMemberReference memberRef in memberRefs)
            {
                MemberRefRow r = new MemberRefRow();
                r.Class = this.GetMemberRefParentCodedIndex(memberRef);
                r.Name = this.GetStringIndexForNameAndCheckLength(memberRef.Name, memberRef);
                r.Signature = this.GetMemberRefSignatureIndex(memberRef);
                _memberRefTable.Add(r);
            }
        }
        
        private void PopulateMethodImplTableRows()
        {
            _methodImplTable.Capacity = this.methodImplList.Count;

            foreach (MethodImplementation methodImplementation in this.methodImplList)
            {
                MethodImplRow r = new MethodImplRow();
                r.Class = (uint)this.GetTypeDefIndex(methodImplementation.ContainingType);
                r.MethodBody = this.GetMethodDefOrRefCodedIndex(methodImplementation.ImplementingMethod);
                r.MethodDecl = this.GetMethodDefOrRefCodedIndex(methodImplementation.ImplementedMethod);
                _methodImplTable.Add(r);
            }
        }

        private void PopulateMethodSemanticsTableRows()
        {
            var propertyDefs = this.GetPropertyDefs();
            var eventDefs = this.GetEventDefs();

            //EDMAURER an estimate, not necessarily accurate.
            _methodSemanticsTable.Capacity = propertyDefs.Count * 2 + eventDefs.Count * 2;

            uint i = 0;
            foreach (IPropertyDefinition propertyDef in this.GetPropertyDefs())
            {
                var r = new MethodSemanticsRow();
                r.Association = GetPropertyDefIndex(propertyDef).ToCodedIndex(HasSemanticsTag.Property);
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

                    r.Method = (uint)this.GetMethodDefIndex(accessorMethod.GetResolvedMethod(Context));
                    r.OriginalIndex = i++;
                    _methodSemanticsTable.Add(r);
                }
            }

            int propertiesOnlyTableCount = _methodSemanticsTable.Count;
            foreach (IEventDefinition eventDef in this.GetEventDefs())
            {
                var r = new MethodSemanticsRow();
                r.Association = GetEventDefIndex(eventDef).ToCodedIndex(HasSemanticsTag.Event);
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

                    r.Method = (uint)this.GetMethodDefIndex(accessorMethod.GetResolvedMethod(Context));
                    r.OriginalIndex = i++;
                    _methodSemanticsTable.Add(r);
                }
            }

            if (_methodSemanticsTable.Count > propertiesOnlyTableCount)
            {
                _methodSemanticsTable.Sort(new MethodSemanticsRowComparer());
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
        
        private void PopulateMethodSpecTableRows()
        {
            var methodSpecs = this.GetMethodSpecs();
            _methodSpecTable.Capacity = methodSpecs.Count;

            foreach (IGenericMethodInstanceReference genericMethodInstanceReference in methodSpecs)
            {
                MethodSpecRow r = new MethodSpecRow();
                r.Method = this.GetMethodDefOrRefCodedIndex(genericMethodInstanceReference.GetGenericMethod(Context));
                r.Instantiation = this.GetGenericMethodInstanceIndex(genericMethodInstanceReference);
                _methodSpecTable.Add(r);
            }
        }
        
        private void PopulateMethodTableRows(int[] methodBodyRvas)
        {
            var methodDefs = this.GetMethodDefs();
            _methodTable = new MethodRow[methodDefs.Count];

            int i = 0;
            foreach (IMethodDefinition methodDef in methodDefs)
            {
                _methodTable[i] = new MethodRow
                {
                    Rva = methodBodyRvas[i],
                    ImplFlags = (ushort)methodDef.GetImplementationAttributes(Context),
                    Flags = GetMethodFlags(methodDef),
                    Name = this.GetStringIndexForNameAndCheckLength(methodDef.Name, methodDef),
                    Signature = this.GetMethodSignatureIndex(methodDef),
                    ParamList = (uint)this.GetParameterDefIndex(methodDef),
                };

                i++;
            }
        }

        private void PopulateModuleRefTableRows()
        {
            var moduleRefs = this.GetModuleRefs();
            _moduleRefTable.Capacity = moduleRefs.Count;

            foreach (string moduleName in moduleRefs)
            {
                ModuleRefRow r = new ModuleRefRow();
                r.Name = this.GetStringIndexForPathAndCheckLength(moduleName);
                _moduleRefTable.Add(r);
            }
        }
        
        private void PopulateModuleTableRow()
        {
            CheckPathLength(this.module.ModuleName);

            // MVID is specified upfront when emitting EnC delta:
            Guid mvid = this.module.Properties.PersistentIdentifier;

            if (mvid == default(Guid) && !_deterministic)
            {
                // If we are being nondeterministic, generate random
                mvid = Guid.NewGuid();
            }

            _moduleRow = MakeModuleRow(heaps, mvid);
        }

        private ModuleRow MakeModuleRow(MetadataHeapsBuilder heaps, Guid mvid)
        {
            return new ModuleRow
            {
                Generation = this.Generation,
                Name = heaps.GetStringIndex(this.module.ModuleName),
                ModuleVersionId = heaps.AllocateGuid(mvid),
                EncId = heaps.GetGuidIndex(this.EncId),
                EncBaseId = heaps.GetGuidIndex(this.EncBaseId),
            };
        }

        private ModuleRow _moduleRow;

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
                uint typeDefIndex = (uint)this.GetTypeDefIndex(typeDef);
                r.NestedClass = typeDefIndex;
                r.EnclosingClass = (uint)this.GetTypeDefIndex(nestedTypeDef.ContainingTypeDefinition);
                _nestedClassTable.Add(r);
            }
        }
        
        private void PopulateParamTableRows()
        {
            var parameterDefs = this.GetParameterDefs();
            _paramTable.Capacity = parameterDefs.Count;

            foreach (IParameterDefinition parDef in parameterDefs)
            {
                ParamRow r = new ParamRow();
                r.Flags = GetParameterFlags(parDef);
                r.Sequence = (ushort)(parDef is ReturnValueParameter ? 0 : parDef.Index + 1);
                r.Name = this.GetStringIndexForNameAndCheckLength(parDef.Name, parDef);
                _paramTable.Add(r);
            }
        }

        private void PopulatePropertyMapTableRows()
        {
            this.PopulatePropertyMapTableRows(_propertyMapTable);
        }

        private void PopulatePropertyTableRows()
        {
            var propertyDefs = this.GetPropertyDefs();
            _propertyTable.Capacity = propertyDefs.Count;

            foreach (IPropertyDefinition propertyDef in propertyDefs)
            {
                var r = new PropertyRow();
                r.PropFlags = GetPropertyFlags(propertyDef);
                r.Name = this.GetStringIndexForNameAndCheckLength(propertyDef.Name, propertyDef);
                r.Type = this.GetPropertySignatureIndex(propertyDef);
                _propertyTable.Add(r);
            }
        }
        
        private void PopulateTypeDefTableRows()
        {
            var typeDefs = this.GetTypeDefs();
            _typeDefTable.Capacity = typeDefs.Count;

            foreach (INamedTypeDefinition typeDef in typeDefs)
            {
                var r = new TypeDefRow();
                INamespaceTypeDefinition namespaceType = typeDef.AsNamespaceTypeDefinition(Context);
                r.Flags = GetTypeDefFlags(typeDef);
                string mangledTypeName = GetMangledName(typeDef);
                r.Name = this.GetStringIndexForNameAndCheckLength(mangledTypeName, typeDef);
                r.Namespace = namespaceType == null
                    ? default(StringIdx)
                    : this.GetStringIndexForNamespaceAndCheckLength(namespaceType, mangledTypeName);
                ITypeReference baseType = typeDef.GetBaseClass(Context);
                r.Extends = (baseType != null) ? this.GetTypeDefOrRefCodedIndex(baseType, true) : 0;

                r.FieldList = (uint)this.GetFieldDefIndex(typeDef);
                r.MethodList = (uint)this.GetMethodDefIndex(typeDef);

                _typeDefTable.Add(r);
            }
        }

        private void PopulateTypeRefTableRows()
        {
            var typeRefs = this.GetTypeRefs();
            _typeRefTable.Capacity = typeRefs.Count;

            foreach (ITypeReference typeRef in typeRefs)
            {
                TypeRefRow r = new TypeRefRow();
                INestedTypeReference nestedTypeRef = typeRef.AsNestedTypeReference;
                if (nestedTypeRef != null)
                {
                    ITypeReference scopeTypeRef;

                    ISpecializedNestedTypeReference sneTypeRef = nestedTypeRef.AsSpecializedNestedTypeReference;
                    if (sneTypeRef != null)
                    {
                        scopeTypeRef = sneTypeRef.UnspecializedVersion.GetContainingType(Context);
                    }
                    else
                    {
                        scopeTypeRef = nestedTypeRef.GetContainingType(Context);
                    }

                    r.ResolutionScope = GetTypeRefIndex(scopeTypeRef).ToCodedIndex(ResolutionScopeTag.TypeRef);
                    r.Name = this.GetStringIndexForNameAndCheckLength(GetMangledName(nestedTypeRef), nestedTypeRef);
                    r.Namespace = default(StringIdx);
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

                _typeRefTable.Add(r);
            }
        }

        private void PopulateTypeSpecTableRows()
        {
            var typeSpecs = this.GetTypeSpecs();
            _typeSpecTable.Capacity = typeSpecs.Count;

            foreach (ITypeReference typeSpec in typeSpecs)
            {
                TypeSpecRow r = new TypeSpecRow();
                r.Signature = this.GetTypeSpecSignatureIndex(typeSpec);
                _typeSpecTable.Add(r);
            }
        }

        private void PopulateStandaloneSignatures()
        {
            var signatures = GetStandAloneSignatures();

            foreach (BlobIdx blob in signatures)
            {
                _standAloneSigTable.Add(new StandaloneSigRow { Signature = blob });
            }
        }

        private int[] SerializeMethodBodies(BlobBuilder ilWriter, PdbWriter pdbWriterOpt)
        {
            CustomDebugInfoWriter customDebugInfoWriter = (pdbWriterOpt != null) ? new CustomDebugInfoWriter(pdbWriterOpt) : null;

            var methods = this.GetMethodDefs();
            int[] rvas = new int[methods.Count];

            int methodRid = 1;
            foreach (IMethodDefinition method in methods)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                int rva;
                IMethodBody body;
                int localSignatureRid;

                if (method.HasBody())
                {
                    body = method.GetBody(Context);
                    Debug.Assert(body != null || allowMissingMethodBodies);

                    if (body != null)
                    {
                        localSignatureRid = this.SerializeLocalVariablesSignature(body);
                        uint localSignatureToken = (localSignatureRid != 0) ? (uint)(0x11000000 | localSignatureRid) : 0;

                        // TODO: consider parallelizing these (local signature tokens can be piped into IL serialization & debug info generation)
                        rva = this.SerializeMethodBody(body, ilWriter, localSignatureToken);

                        pdbWriterOpt?.SerializeDebugInfo(body, localSignatureToken, customDebugInfoWriter);
                    }
                    else
                    {
                        rva = 0;
                        localSignatureRid = 0;
                    }
                }
                else
                {
                    // 0 is actually written to metadata when the row is serialized
                    rva = -1;
                    body = null;
                    localSignatureRid = 0;
                }

                if (_debugHeapsOpt != null)
                {
                    SerializeMethodDebugInfo(body, methodRid, localSignatureRid);
                }

                rvas[methodRid - 1] = rva;

                methodRid++;
            }

            return rvas;
        }

        private int SerializeMethodBody(IMethodBody methodBody, BlobBuilder ilWriter, uint localSignatureToken)
        {
            int ilLength = methodBody.IL.Length;
            uint numberOfExceptionHandlers = (uint)methodBody.ExceptionRegions.Length;
            bool isSmallBody = ilLength < 64 && methodBody.MaxStack <= 8 && localSignatureToken == 0 && numberOfExceptionHandlers == 0;

            // Check if an identical method body has already been serialized. 
            // If so, use the RVA of the already serialized one.
            // Note that we don't need to rewrite the fake tokens in the body before looking it up.
            int bodyRva;
            if (isSmallBody && _smallMethodBodies.TryGetValue(methodBody.IL, out bodyRva))
            {
                return bodyRva;
            }

            if (isSmallBody)
            {
                bodyRva = ilWriter.Position;
                ilWriter.WriteByte((byte)((ilLength << 2) | 2));

                _smallMethodBodies.Add(methodBody.IL, bodyRva);
            }
            else
            {
                ilWriter.Align(4);

                bodyRva = ilWriter.Position;

                ushort flags = (3 << 12) | 0x3;
                if (numberOfExceptionHandlers > 0)
                {
                    flags |= 0x08;
                }

                if (methodBody.LocalsAreZeroed)
                {
                    flags |= 0x10;
                }

                ilWriter.WriteUInt16(flags);
                ilWriter.WriteUInt16(methodBody.MaxStack);
                ilWriter.WriteUInt32((uint)ilLength);
                ilWriter.WriteUInt32(localSignatureToken);
            }

            WriteMethodBodyIL(ilWriter, methodBody);

            if (numberOfExceptionHandlers > 0)
            {
                SerializeMethodBodyExceptionHandlerTable(methodBody, numberOfExceptionHandlers, ilWriter);
            }

            return bodyRva;
        }

        /// <summary>
        /// Serialize the method local signature to the blob.
        /// </summary>
        /// <returns>Standalone signature token</returns>
        protected virtual int SerializeLocalVariablesSignature(IMethodBody body)
        {
            Debug.Assert(!_tableIndicesAreComplete);

            var localVariables = body.LocalVariables;
            if (localVariables.Length == 0)
            {
                return 0;
            }

            var writer = PooledBlobBuilder.GetInstance();
            writer.WriteByte(0x07);
            writer.WriteCompressedInteger((uint)localVariables.Length);
            foreach (ILocalDefinition local in localVariables)
            {
                this.SerializeLocalVariableSignature(writer, local);
            }

            BlobIdx blobIndex = heaps.GetBlobIndex(writer);
            int signatureIndex = this.GetOrAddStandAloneSignatureIndex(blobIndex);
            writer.Free();

            return signatureIndex;
        }

        protected void SerializeLocalVariableSignature(BlobBuilder writer, ILocalDefinition local)
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

        internal int SerializeLocalConstantStandAloneSignature(ILocalDefinition localConstant)
        {
            var writer = PooledBlobBuilder.GetInstance();
            writer.WriteByte(0x06);

            foreach (ICustomModifier modifier in localConstant.CustomModifiers)
            {
                this.SerializeCustomModifier(modifier, writer);
            }

            this.SerializeTypeReference(localConstant.Type, writer, false, true);
            BlobIdx blobIndex = heaps.GetBlobIndex(writer);
            int signatureIndex = GetOrAddStandAloneSignatureIndex(blobIndex);
            writer.Free();

            return 0x11000000 | signatureIndex;
        }

        private static int ReadInt32(ImmutableArray<byte> buffer, int pos)
        {
            return buffer[pos] | buffer[pos + 1] << 8 | buffer[pos + 2] << 16 | buffer[pos + 3] << 24;
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

        private int ResolveTokenFromReference(IReference reference)
        {
            ITypeReference typeReference = reference as ITypeReference;

            if (typeReference != null)
            {
                return this.GetTypeToken(typeReference);
            }

            IFieldReference fieldReference = reference as IFieldReference;

            if (fieldReference != null)
            {
                return this.GetFieldToken(fieldReference);
            }

            IMethodReference methodReference = reference as IMethodReference;
            if (methodReference != null)
            {
                return this.GetMethodToken(methodReference);
            }

            throw ExceptionUtilities.UnexpectedValue(reference);
        }

        private int ResolveSymbolTokenFromPseudoSymbolToken(int pseudoSymbolToken)
        {
            int index = pseudoSymbolToken;
            var reference = _pseudoSymbolTokenToReferenceMap[index];
            if (reference != null)
            {
                // EDMAURER since method bodies are not visited as they are in CCI, the operations
                // that would have been done on them are done here.
                _referenceVisitor.VisitMethodBodyReference(reference);

                int token = ResolveTokenFromReference(reference);
                _pseudoSymbolTokenToTokenMap[index] = token;
                _pseudoSymbolTokenToReferenceMap[index] = null; // Set to null to bypass next lookup
                return token;
            }

            return _pseudoSymbolTokenToTokenMap[index];
        }

        private int ResolveStringTokenFromPseudoStringToken(int pseudoStringToken)
        {
            int index = pseudoStringToken;
            var str = _pseudoStringTokenToStringMap[index];
            if (str != null)
            {
                var token = heaps.GetUserStringToken(str);
                _pseudoStringTokenToTokenMap[index] = token;
                _pseudoStringTokenToStringMap[index] = null; // Set to null to bypass next lookup
                return token;
            }

            return _pseudoStringTokenToTokenMap[index];
        }

        private void WriteMethodBodyIL(BlobBuilder builder, IMethodBody methodBody)
        {
            ImmutableArray<byte> methodBodyIL = methodBody.IL;

            // write the raw body first and then patch tokens:
            var writer = builder.ReserveBytes(methodBodyIL.Length);
            writer.WriteBytes(methodBodyIL);

            int offset = 0;
            while (offset < methodBodyIL.Length)
            {
                var operandType = InstructionOperandTypes.ReadOperandType(methodBodyIL, ref offset);
                switch (operandType)
                {
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        writer.Offset = offset;
                        writer.WriteInt32(ResolveSymbolTokenFromPseudoSymbolToken(ReadInt32(methodBodyIL, offset)));
                        offset += 4;
                        break;

                    case OperandType.InlineString:
                        writer.Offset = offset;
                        writer.WriteInt32(ResolveStringTokenFromPseudoStringToken(ReadInt32(methodBodyIL, offset)));
                        offset += 4;
                        break;

                    case OperandType.InlineSig: // calli
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineI:
                    case OperandType.ShortInlineR:
                        offset += 4;
                        break;

                    case OperandType.InlineSwitch:
                        int argCount = ReadInt32(methodBodyIL, offset);
                        // skip switch arguments count and arguments
                        offset += (argCount + 1) * 4;
                        break;

                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        offset += 8;
                        break;

                    case OperandType.InlineNone:
                        break;

                    case OperandType.InlineVar:
                        offset += 2;
                        break;

                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        offset += 1;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(operandType);
                }
            }
        }

        private void SerializeMethodBodyExceptionHandlerTable(IMethodBody methodBody, uint numberOfExceptionHandlers, BlobBuilder writer)
        {
            var regions = methodBody.ExceptionRegions;
            bool useSmallExceptionHeaders = MayUseSmallExceptionHeaders(numberOfExceptionHandlers, regions);
            writer.Align(4);
            if (useSmallExceptionHeaders)
            {
                uint dataSize = numberOfExceptionHandlers * 12 + 4;
                writer.WriteByte(0x01);
                writer.WriteByte((byte)(dataSize & 0xff));
                writer.WriteUInt16(0);
            }
            else
            {
                uint dataSize = numberOfExceptionHandlers * 24 + 4;
                writer.WriteByte(0x41);
                writer.WriteByte((byte)(dataSize & 0xff));
                writer.WriteUInt16((ushort)((dataSize >> 8) & 0xffff));
            }

            foreach (var region in regions)
            {
                this.SerializeExceptionRegion(region, useSmallExceptionHeaders, writer);
            }
        }

        private void SerializeExceptionRegion(ExceptionHandlerRegion region, bool useSmallExceptionHeaders, BlobBuilder writer)
        {
            writer.WriteUInt16((ushort)region.HandlerKind);

            if (useSmallExceptionHeaders)
            {
                writer.WriteUInt16((ushort)region.TryStartOffset);
                writer.WriteByte((byte)(region.TryEndOffset - region.TryStartOffset));
                writer.WriteUInt16((ushort)region.HandlerStartOffset);
                writer.WriteByte((byte)(region.HandlerEndOffset - region.HandlerStartOffset));
            }
            else
            {
                writer.WriteUInt16(0);
                writer.WriteUInt32((uint)region.TryStartOffset);
                writer.WriteUInt32((uint)(region.TryEndOffset - region.TryStartOffset));
                writer.WriteUInt32((uint)region.HandlerStartOffset);
                writer.WriteUInt32((uint)(region.HandlerEndOffset - region.HandlerStartOffset));
            }

            if (region.HandlerKind == ExceptionRegionKind.Catch)
            {
                writer.WriteUInt32((uint)this.GetTypeToken(region.ExceptionType));
            }
            else
            {
                writer.WriteUInt32((uint)region.FilterDecisionStartOffset);
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

        private void SerializeParameterInformation(IParameterTypeInformation parameterTypeInformation, BlobBuilder writer)
        {
            ushort countOfCustomModifiersPrecedingByRef = parameterTypeInformation.CountOfCustomModifiersPrecedingByRef;
            var modifiers = parameterTypeInformation.CustomModifiers;

            Debug.Assert(countOfCustomModifiersPrecedingByRef == 0 || parameterTypeInformation.IsByReference);

            if (parameterTypeInformation.IsByReference)
            {
                for (int i = 0; i < countOfCustomModifiersPrecedingByRef; i++)
                {
                    this.SerializeCustomModifier(modifiers[i], writer);
                }

                writer.WriteByte(0x10);
            }

            for (int i = countOfCustomModifiersPrecedingByRef; i < modifiers.Length; i++)
            {
                this.SerializeCustomModifier(modifiers[i], writer);
            }

            this.SerializeTypeReference(parameterTypeInformation.GetType(Context), writer, false, true);
        }

        private void SerializeFieldSignature(IFieldReference fieldReference, BlobBuilder writer)
        {
            writer.WriteByte(0x06);

            this.SerializeTypeReference(fieldReference.GetType(Context), writer, false, true);
        }

        private void SerializeGenericMethodInstanceSignature(BlobBuilder writer, IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            writer.WriteByte(0x0a);
            writer.WriteCompressedInteger(genericMethodInstanceReference.GetGenericMethod(Context).GenericParameterCount);
            foreach (ITypeReference genericArgument in genericMethodInstanceReference.GetGenericArguments(Context))
            {
                this.SerializeTypeReference(genericArgument, writer, false, true);
            }
        }

        private void SerializeCustomAttributeSignature(ICustomAttribute customAttribute, bool writeOnlyNamedArguments, BlobBuilder writer)
        {
            if (!writeOnlyNamedArguments)
            {
                writer.WriteUInt16(0x0001);
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

                writer.WriteUInt16(customAttribute.NamedArgumentCount);
            }
            else
            {
                writer.WriteCompressedInteger(customAttribute.NamedArgumentCount);
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

                    writer.WriteSerializedString(namedArgument.ArgumentName);

                    this.SerializeMetadataExpression(writer, namedArgument.ArgumentValue, namedArgument.Type);
                }
            }
        }

        private void SerializeMetadataExpression(BlobBuilder writer, IMetadataExpression expression, ITypeReference targetType)
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

                writer.WriteUInt32(a.ElementCount);

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

                    this.SerializeTypeReference(expression.Type, writer, true, true);
                }

                if (c != null)
                {
                    if (c.Type is IArrayTypeReference)
                    {
                        writer.WriteInt32(-1); // null array
                    }
                    else if (c.Type.TypeCode(Context) == PrimitiveTypeCode.String)
                    {
                        writer.WriteSerializedString((string)c.Value);
                    }
                    else if (this.module.IsPlatformType(c.Type, PlatformType.SystemType))
                    {
                        Debug.Assert(c.Value == null);
                        writer.WriteSerializedString(null);
                    }
                    else
                    {
                        writer.WriteConstant(c.Value);
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

        private void SerializeMarshallingDescriptor(IMarshallingInformation marshallingInformation, BlobBuilder writer)
        {
            writer.WriteCompressedInteger((uint)marshallingInformation.UnmanagedType);
            switch (marshallingInformation.UnmanagedType)
            {
                case UnmanagedType.ByValArray: // NATIVE_TYPE_FIXEDARRAY
                    Debug.Assert(marshallingInformation.NumberOfElements >= 0);
                    writer.WriteCompressedInteger((uint)marshallingInformation.NumberOfElements);
                    if (marshallingInformation.ElementType >= 0)
                    {
                        writer.WriteCompressedInteger((uint)marshallingInformation.ElementType);
                    }

                    break;

                case Constants.UnmanagedType_CustomMarshaler:
                    writer.WriteUInt16(0); // padding

                    object marshaller = marshallingInformation.GetCustomMarshaller(Context);
                    ITypeReference marshallerTypeRef = marshaller as ITypeReference;
                    if (marshallerTypeRef != null)
                    {
                        this.SerializeTypeName(marshallerTypeRef, writer);
                    }
                    else if (marshaller != null)
                    {
                        writer.WriteSerializedString((string)marshaller);
                    }
                    else
                    {
                        writer.WriteByte(0);
                    }

                    var arg = marshallingInformation.CustomMarshallerRuntimeArgument;
                    if (arg != null)
                    {
                        writer.WriteSerializedString(arg);
                    }
                    else
                    {
                        writer.WriteByte(0);
                    }

                    break;

                case UnmanagedType.LPArray: // NATIVE_TYPE_ARRAY
                    Debug.Assert(marshallingInformation.ElementType >= 0);
                    writer.WriteCompressedInteger((uint)marshallingInformation.ElementType);
                    if (marshallingInformation.ParamIndex >= 0)
                    {
                        writer.WriteCompressedInteger((uint)marshallingInformation.ParamIndex);
                        if (marshallingInformation.NumberOfElements >= 0)
                        {
                            writer.WriteCompressedInteger((uint)marshallingInformation.NumberOfElements);
                            writer.WriteByte(1); // The parameter number is valid
                        }
                    }
                    else if (marshallingInformation.NumberOfElements >= 0)
                    {
                        writer.WriteByte(0); // Dummy parameter value emitted so that NumberOfElements can be in a known position
                        writer.WriteCompressedInteger((uint)marshallingInformation.NumberOfElements);
                        writer.WriteByte(0); // The parameter number is not valid
                    }

                    break;

                case UnmanagedType.SafeArray:
                    if (marshallingInformation.SafeArrayElementSubtype >= 0)
                    {
                        writer.WriteCompressedInteger((uint)marshallingInformation.SafeArrayElementSubtype);
                        var elementType = marshallingInformation.GetSafeArrayElementUserDefinedSubtype(Context);
                        if (elementType != null)
                        {
                            this.SerializeTypeName(elementType, writer);
                        }
                    }

                    break;

                case UnmanagedType.ByValTStr: // NATIVE_TYPE_FIXEDSYSSTRING
                    writer.WriteCompressedInteger((uint)marshallingInformation.NumberOfElements);
                    break;

                case UnmanagedType.Interface:
                case UnmanagedType.IDispatch:
                case UnmanagedType.IUnknown:
                    if (marshallingInformation.IidParameterIndex >= 0)
                    {
                        writer.WriteCompressedInteger((uint)marshallingInformation.IidParameterIndex);
                    }

                    break;
            }
        }

        private void SerializeTypeName(ITypeReference typeReference, BlobBuilder writer)
        {
            writer.WriteSerializedString(typeReference.GetSerializedTypeName(this.Context));
        }

        /// <summary>
        /// Computes the string representing the strong name of the given assembly reference.
        /// </summary>
        internal static string StrongName(IAssemblyReference assemblyReference)
        {
            var pooled = PooledStringBuilder.GetInstance();
            StringBuilder sb = pooled.Builder;
            sb.Append(assemblyReference.Name);
            sb.AppendFormat(CultureInfo.InvariantCulture, ", Version={0}.{1}.{2}.{3}", assemblyReference.Version.Major, assemblyReference.Version.Minor, assemblyReference.Version.Build, assemblyReference.Version.Revision);
            if (!string.IsNullOrEmpty(assemblyReference.Culture))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", Culture={0}", assemblyReference.Culture);
            }
            else
            {
                sb.Append(", Culture=neutral");
            }

            sb.Append(", PublicKeyToken=");
            if (assemblyReference.PublicKeyToken.Length > 0)
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

            if (assemblyReference.ContentType == AssemblyContentType.WindowsRuntime)
            {
                sb.Append(", ContentType=WindowsRuntime");
            }
            else
            {
                Debug.Assert(assemblyReference.ContentType == AssemblyContentType.Default);
            }

            return pooled.ToStringAndFree();
        }

        private void SerializePermissionSet(IEnumerable<ICustomAttribute> permissionSet, BlobBuilder writer)
        {
            EmitContext context = this.Context;
            foreach (ICustomAttribute customAttribute in permissionSet)
            {
                bool isAssemblyQualified = true;
                string typeName = customAttribute.GetType(context).GetSerializedTypeName(context, ref isAssemblyQualified);
                if (!isAssemblyQualified)
                {
                    INamespaceTypeReference namespaceType = customAttribute.GetType(context).AsNamespaceTypeReference;
                    var referencedAssembly = namespaceType?.GetUnit(context) as IAssemblyReference;
                    if (referencedAssembly != null)
                    {
                        typeName = typeName + ", " + StrongName(referencedAssembly);
                    }
                }

                writer.WriteSerializedString(typeName);
                var customAttributeWriter = PooledBlobBuilder.GetInstance();
                this.SerializeCustomAttributeSignature(customAttribute, true, customAttributeWriter);
                writer.WriteCompressedInteger((uint)customAttributeWriter.Count);
                customAttributeWriter.WriteContentTo(writer);
                customAttributeWriter.Free();
            }
            // TODO: xml for older platforms
        }

        private void SerializeSignature(ISignature signature, ushort genericParameterCount, ImmutableArray<IParameterTypeInformation> extraArgumentTypes, BlobBuilder writer)
        {
            byte header = (byte)signature.CallingConvention;
            if (signature is IPropertyDefinition)
            {
                header |= 0x08;
            }

            writer.WriteByte(header);
            if (genericParameterCount > 0)
            {
                writer.WriteCompressedInteger(genericParameterCount);
            }

            var @params = signature.GetParameters(Context);
            uint numberOfRequiredParameters = (uint)@params.Length;
            uint numberOfOptionalParameters = (uint)extraArgumentTypes.Length;
            writer.WriteCompressedInteger(numberOfRequiredParameters + numberOfOptionalParameters);

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

        private void SerializeTypeReference(ITypeReference typeReference, BlobBuilder writer, bool noTokens, bool treatRefAsPotentialTypeSpec)
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

                            writer.WriteByte(0x0f);
                            typeReference = pointerTypeReference.GetTargetType(Context);
                            treatRefAsPotentialTypeSpec = true;
                            continue;
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

                            writer.WriteByte(0x10);
                            typeReference = managedPointerTypeReference.GetTargetType(Context);
                            treatRefAsPotentialTypeSpec = true;
                            continue;
                        }

                        break;
                    case PrimitiveTypeCode.IntPtr:
                        writer.WriteByte(0x18);
                        return;
                    case PrimitiveTypeCode.UIntPtr:
                        writer.WriteByte(0x19);
                        return;
                }


                IGenericTypeParameterReference genericTypeParameterReference = typeReference.AsGenericTypeParameterReference;
                if (genericTypeParameterReference != null)
                {
                    writer.WriteByte(0x13);
                    uint numberOfInheritedParameters = GetNumberOfInheritedTypeParameters(genericTypeParameterReference.DefiningType);
                    writer.WriteCompressedInteger(numberOfInheritedParameters + genericTypeParameterReference.Index);
                    return;
                }

                var arrayTypeReference = typeReference as IArrayTypeReference;
                if (arrayTypeReference?.IsSZArray == false)
                {
                    Debug.Assert(noTokens == false, "Custom attributes cannot have multi-dimensional arrays");

                    writer.WriteByte(0x14);
                    this.SerializeTypeReference(arrayTypeReference.GetElementType(Context), writer, false, true);
                    writer.WriteCompressedInteger(arrayTypeReference.Rank);
                    writer.WriteCompressedInteger(IteratorHelper.EnumerableCount(arrayTypeReference.Sizes));
                    foreach (ulong size in arrayTypeReference.Sizes)
                    {
                        writer.WriteCompressedInteger((uint)size);
                    }

                    writer.WriteCompressedInteger(IteratorHelper.EnumerableCount(arrayTypeReference.LowerBounds));
                    foreach (int lowerBound in arrayTypeReference.LowerBounds)
                    {
                        writer.WriteCompressedSignedInteger(lowerBound);
                    }

                    return;
                }

                if (module.IsPlatformType(typeReference, PlatformType.SystemTypedReference))
                {
                    writer.WriteByte(0x16);
                    return;
                }

                if (module.IsPlatformType(typeReference, PlatformType.SystemObject))
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

                if (arrayTypeReference != null && arrayTypeReference.IsSZArray)
                {
                    writer.WriteByte(0x1d);
                    typeReference = arrayTypeReference.GetElementType(Context);
                    treatRefAsPotentialTypeSpec = true;
                    continue;
                }

                IGenericMethodParameterReference genericMethodParameterReference = typeReference.AsGenericMethodParameterReference;
                if (genericMethodParameterReference != null)
                {
                    writer.WriteByte(0x1e);
                    writer.WriteCompressedInteger(genericMethodParameterReference.Index);
                    return;
                }

                if (!noTokens && typeReference.IsTypeSpecification() && treatRefAsPotentialTypeSpec)
                {
                    ITypeReference uninstantiatedTypeReference = typeReference.GetUninstantiatedGenericType();

                    // Roslyn's uninstantiated type is the same object as the instantiated type for
                    // types closed over their type parameters, so to speak.

                    writer.WriteByte(0x15);
                    this.SerializeTypeReference(uninstantiatedTypeReference, writer, false, false);
                    var consolidatedTypeArguments = ArrayBuilder<ITypeReference>.GetInstance();
                    typeReference.GetConsolidatedTypeArguments(consolidatedTypeArguments, this.Context);
                    writer.WriteCompressedInteger((uint)consolidatedTypeArguments.Count);
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

                    writer.WriteCompressedInteger(this.GetTypeDefOrRefCodedIndex(typeReference, treatRefAsPotentialTypeSpec));
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

        internal static EditAndContinueMethodDebugInformation GetEncMethodDebugInfo(IMethodBody methodBody)
        {
            ImmutableArray<LocalSlotDebugInfo> encLocalSlots;

            // Kickoff method of a state machine (async/iterator method) doesn't have any interesting locals,
            // so we use its EnC method debug info to store information about locals hoisted to the state machine.
            var encSlotInfo = methodBody.StateMachineHoistedLocalSlots;
            if (encSlotInfo.IsDefault)
            {
                encLocalSlots = GetLocalSlotDebugInfos(methodBody.LocalVariables);
            }
            else
            {
                encLocalSlots = GetLocalSlotDebugInfos(encSlotInfo);
            }

            return new EditAndContinueMethodDebugInformation(methodBody.MethodId.Ordinal, encLocalSlots, methodBody.ClosureDebugInfo, methodBody.LambdaDebugInfo);
        }

        internal static ImmutableArray<LocalSlotDebugInfo> GetLocalSlotDebugInfos(ImmutableArray<ILocalDefinition> locals)
        {
            if (!locals.Any(variable => !variable.SlotInfo.Id.IsNone))
            {
                return ImmutableArray<LocalSlotDebugInfo>.Empty;
            }

            return locals.SelectAsArray(variable => variable.SlotInfo);
        }

        internal static ImmutableArray<LocalSlotDebugInfo> GetLocalSlotDebugInfos(ImmutableArray<EncHoistedLocalInfo> locals)
        {
            if (!locals.Any(variable => !variable.SlotInfo.Id.IsNone))
            {
                return ImmutableArray<LocalSlotDebugInfo>.Empty;
            }

            return locals.SelectAsArray(variable => variable.SlotInfo);
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
            private readonly MetadataWriter _writer;
            private readonly List<T> _rows;
            private readonly int _firstRowId;

            protected HeapOrReferenceIndexBase(MetadataWriter writer, int lastRowId)
            {
                _writer = writer;
                _rows = new List<T>();
                _firstRowId = lastRowId + 1;
            }

            public abstract bool TryGetValue(T item, out int index);

            public int GetOrAdd(T item)
            {
                int index;
                if (!this.TryGetValue(item, out index))
                {
                    index = Add(item);
                }

                return index;
            }

            public IReadOnlyList<T> Rows
            {
                get { return _rows; }
            }

            public int Add(T item)
            {
                Debug.Assert(!_writer._tableIndicesAreComplete);
#if DEBUG
                int i;
                Debug.Assert(!this.TryGetValue(item, out i));
#endif
                int index = _firstRowId + _rows.Count;
                this.AddItem(item, index);
                _rows.Add(item);
                return index;
            }

            protected abstract void AddItem(T item, int index);
        }

        protected sealed class HeapOrReferenceIndex<T> : HeapOrReferenceIndexBase<T>
        {
            private readonly Dictionary<T, int> _index;

            public HeapOrReferenceIndex(MetadataWriter writer, int lastRowId = 0)
                : this(writer, new Dictionary<T, int>(), lastRowId)
            {
            }

            public HeapOrReferenceIndex(MetadataWriter writer, IEqualityComparer<T> comparer, int lastRowId = 0)
                : this(writer, new Dictionary<T, int>(comparer), lastRowId)
            {
            }

            private HeapOrReferenceIndex(MetadataWriter writer, Dictionary<T, int> index, int lastRowId)
                : base(writer, lastRowId)
            {
                Debug.Assert(index.Count == 0);
                _index = index;
            }

            public override bool TryGetValue(T item, out int index)
            {
                return _index.TryGetValue(item, out index);
            }

            protected override void AddItem(T item, int index)
            {
                _index.Add(item, index);
            }
        }

        protected sealed class InstanceAndStructuralReferenceIndex<T> : HeapOrReferenceIndexBase<T> where T : IReference
        {
            private readonly Dictionary<T, int> _instanceIndex;
            private readonly Dictionary<T, int> _structuralIndex;

            public InstanceAndStructuralReferenceIndex(MetadataWriter writer, IEqualityComparer<T> structuralComparer, int lastRowId = 0)
                : base(writer, lastRowId)
            {
                _instanceIndex = new Dictionary<T, int>();
                _structuralIndex = new Dictionary<T, int>(structuralComparer);
            }

            public override bool TryGetValue(T item, out int index)
            {
                if (_instanceIndex.TryGetValue(item, out index))
                {
                    return true;
                }
                if (_structuralIndex.TryGetValue(item, out index))
                {
                    _instanceIndex.Add(item, index);
                    return true;
                }
                return false;
            }

            protected override void AddItem(T item, int index)
            {
                _instanceIndex.Add(item, index);
                _structuralIndex.Add(item, index);
            }
        }
    }
}
